using CmsImporter.Application.Telemetry;
using CmsImporter.Core.Abstractions;
using CmsImporter.Core.DTOs;
using CmsImporter.Core.Entities;

using Microsoft.Extensions.Logging;

namespace CmsImporter.Application.Pipeline;

/// <summary>
/// Persists a batch of <see cref="ContentItem"/> via <see cref="IContentRepository.UpsertManyAsync"/>,
/// updates progress counters, and emits an <c>Import.Load</c> tracing span. The stage is scoped
/// (per-import-job DI scope) because it transitively depends on the scoped <c>DbContext</c>.
/// </summary>
public sealed class LoadStage(
    IContentRepository repository,
    ILogger<LoadStage> logger)
{
    /// <summary>
    /// Calls <see cref="IContentRepository.UpsertManyAsync"/> for the batch and reports the
    /// new/updated split back to <see cref="ImportProgress"/>. Returns the
    /// <see cref="UpsertResult"/> for downstream notification.
    /// </summary>
    public async Task<UpsertResult> ExecuteAsync(
        IReadOnlyCollection<ContentItem> batch,
        ImportProgress progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(batch);

        if (batch.Count == 0)
        {
            return UpsertResult.Empty;
        }

        using var activity = ImportActivitySource.Instance.StartActivity("Import.Load");
        activity?.SetTag("import.batch_size", batch.Count);

        var result = await repository.UpsertManyAsync(batch, cancellationToken);

        progress.AddLoaded(result.NewItems.Count, result.UpdatedItems.Count);

        activity?.SetTag("import.new_items", result.NewItems.Count);
        activity?.SetTag("import.updated_items", result.UpdatedItems.Count);

        logger.LogInformation(
            "Loaded batch: {NewCount} new, {UpdatedCount} updated for job {JobId}",
            result.NewItems.Count, result.UpdatedItems.Count, progress.JobId);

        return result;
    }
}
