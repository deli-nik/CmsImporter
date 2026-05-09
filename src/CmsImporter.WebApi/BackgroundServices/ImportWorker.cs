using System.Threading.Channels;

using CmsImporter.Application.Abstractions;
using CmsImporter.Application.Pipeline;

namespace CmsImporter.WebApi.BackgroundServices;

public sealed class ImportWorker(
    Channel<ImportJob> channel,
    IServiceScopeFactory scopeFactory,
    ILogger<ImportWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("ImportWorker started; waiting for jobs.");

        try
        {
            await foreach (var job in channel.Reader.ReadAllAsync(stoppingToken))
            {
                await RunJobAsync(job, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("ImportWorker stopping (cancellation requested).");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "ImportWorker terminated unexpectedly.");
            throw;
        }
    }

    private async Task RunJobAsync(ImportJob job, CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Picked up import job {JobId} for source {SourceConnector}",
            job.Id, job.SourceConnector);

        // One DI scope per job: scoped DbContext + repository live for this job only.
        await using var scope = scopeFactory.CreateAsyncScope();

        var orchestrator = scope.ServiceProvider.GetRequiredService<ImportOrchestrator>();
        var tracker = scope.ServiceProvider.GetRequiredService<IImportProgressTracker>();

        var progress = tracker.Get(job.Id)
            ?? throw new InvalidOperationException(
                $"Progress entry missing for job {job.Id}; the API endpoint should have registered it.");

        try
        {
            await orchestrator.RunAsync(job, progress, stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Import job {JobId} threw out of orchestrator.", job.Id);
        }
    }
}
