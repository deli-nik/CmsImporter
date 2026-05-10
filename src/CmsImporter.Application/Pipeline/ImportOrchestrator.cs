using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Channels;

using CmsImporter.Application.Telemetry;
using CmsImporter.Core.Entities;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CmsImporter.Application.Pipeline;

public sealed class ImportOrchestrator(
    ExtractStage extractStage,
    TransformStage transformStage,
    ValidateStage validateStage,
    LoadStage loadStage,
    NotifyStage notifyStage,
    IOptions<ImportOrchestratorOptions> options,
    TimeProvider timeProvider,
    ILogger<ImportOrchestrator> logger)
{
    private readonly ImportOrchestratorOptions _options = options.Value;

    public async Task<ImportResult> RunAsync(
        ImportJob job,
        ImportProgress progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(progress);

        using var rootActivity = ImportActivitySource.Instance.StartActivity("Import.Run");
        rootActivity?.SetTag("import.job_id", job.Id);
        rootActivity?.SetTag("import.source", job.SourceConnector);

        var startedAt = timeProvider.GetUtcNow();
        progress.Status = ImportStatus.Running;
        progress.StartedAt = startedAt;
        var stopwatch = Stopwatch.StartNew();

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var channel = Channel.CreateBounded<ContentItem>(new BoundedChannelOptions(_options.ChannelCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false,
        });

        var producerTask = ProduceAsync(job, progress, channel.Writer, linkedCts.Token);
        var consumerTask = ConsumeAsync(progress, channel.Reader, linkedCts.Token);

        try
        {
            await Task.WhenAll(producerTask, consumerTask);

            stopwatch.Stop();
            progress.Status = ImportStatus.Completed;
            progress.CompletedAt = timeProvider.GetUtcNow();

            logger.LogInformation(
                "Import {JobId} completed in {ElapsedMs}ms — extracted={Extracted}, transformed={Transformed}, validationFailed={Failed}, loaded={Loaded} (new={New}, updated={Updated}), notified={Notified}",
                job.Id, stopwatch.ElapsedMilliseconds,
                progress.ItemsExtracted, progress.ItemsTransformed, progress.ItemsValidationFailed,
                progress.ItemsLoaded, progress.NewItems, progress.UpdatedItems, progress.ItemsNotified);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            progress.Status = ImportStatus.Cancelled;
            progress.CompletedAt = timeProvider.GetUtcNow();
            progress.FailureReason = "Cancelled by caller.";
            logger.LogWarning("Import {JobId} cancelled.", job.Id);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            progress.Status = ImportStatus.Failed;
            progress.CompletedAt = timeProvider.GetUtcNow();
            progress.FailureReason = ex.Message;
            progress.RecordError(ex.ToString());
            await linkedCts.CancelAsync();
            rootActivity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            logger.LogError(ex, "Import {JobId} failed", job.Id);
        }

        return BuildResult(progress, stopwatch.Elapsed);
    }

    private async Task ProduceAsync(
        ImportJob job,
        ImportProgress progress,
        ChannelWriter<ContentItem> writer,
        CancellationToken cancellationToken)
    {
        Exception? failure = null;

        try
        {
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = _options.TransformParallelism,
                CancellationToken = cancellationToken,
            };

            await Parallel.ForEachAsync(
                extractStage.ExecuteAsync(job, progress, cancellationToken),
                parallelOptions,
                async (raw, ct) =>
                {
                    using var transformActivity = ImportActivitySource.Instance.StartActivity("Import.TransformOne");

                    ContentItem candidate;
                    try
                    {
                        candidate = transformStage.Execute(raw);
                    }
                    catch (Exception ex)
                    {
                        progress.IncrementValidationFailed();
                        progress.RecordError($"Transform failed for {raw.SourceSystem}::{raw.ExternalId}: {ex.Message}");
                        return;
                    }

                    var validation = validateStage.Execute(candidate);
                    if (!validation.IsValid)
                    {
                        progress.IncrementValidationFailed();
                        foreach (var err in validation.Errors)
                        {
                            progress.RecordError($"{candidate.SourceSystem}::{candidate.ExternalId}: {err}");
                        }
                        return;
                    }

                    progress.IncrementTransformed();
                    await writer.WriteAsync(candidate, ct);
                });
        }
        catch (Exception ex)
        {
            failure = ex;
            throw;
        }
        finally
        {
            writer.Complete(failure);
        }
    }

    private async Task ConsumeAsync(
        ImportProgress progress,
        ChannelReader<ContentItem> reader,
        CancellationToken cancellationToken)
    {
        // For deduplication
        var batch = new ConcurrentDictionary<string, ContentItem>(
            concurrencyLevel: 1, capacity: _options.LoadBatchSize);

        await foreach (var item in reader.ReadAllAsync(cancellationToken))
        {
            var key = $"{item.SourceSystem}::{item.ExternalId}";
            batch[key] = item;

            if (batch.Count >= _options.LoadBatchSize)
            {
                await FlushAsync(batch, progress, cancellationToken);
            }
        }

        if (batch.Count > 0)
        {
            await FlushAsync(batch, progress, cancellationToken);
        }
    }

    private async Task FlushAsync(
        ConcurrentDictionary<string, ContentItem> batch,
        ImportProgress progress,
        CancellationToken cancellationToken)
    {
        var snapshot = batch.Values.ToArray();
        batch.Clear();

        var upsertResult = await loadStage.ExecuteAsync(snapshot, progress, cancellationToken);
        await notifyStage.ExecuteAsync(upsertResult, progress, cancellationToken);
    }

    private static ImportResult BuildResult(ImportProgress progress, TimeSpan duration) =>
        new()
        {
            JobId = progress.JobId,
            Status = progress.Status,
            ItemsExtracted = progress.ItemsExtracted,
            ItemsTransformed = progress.ItemsTransformed,
            ItemsValidationFailed = progress.ItemsValidationFailed,
            ItemsLoaded = progress.ItemsLoaded,
            NewItems = progress.NewItems,
            UpdatedItems = progress.UpdatedItems,
            ItemsNotified = progress.ItemsNotified,
            Duration = duration,
            FailureReason = progress.FailureReason,
            Errors = progress.Errors.ToArray(),
        };
}
