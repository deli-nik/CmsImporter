using CmsImporter.Application.Telemetry;
using CmsImporter.Core.Abstractions;
using CmsImporter.Core.DTOs;
using CmsImporter.Core.Entities;

using Microsoft.Extensions.Logging;

namespace CmsImporter.Application.Pipeline;

public sealed class LoadStage(
    IContentRepository repository,
    ILogger<LoadStage> logger)
{
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
