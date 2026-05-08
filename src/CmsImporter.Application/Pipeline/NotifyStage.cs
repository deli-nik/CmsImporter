using CmsImporter.Application.Telemetry;
using CmsImporter.Core.Abstractions;
using CmsImporter.Core.DTOs;
using CmsImporter.Core.Entities;
using CmsImporter.Core.Events;

using Microsoft.Extensions.Logging;

namespace CmsImporter.Application.Pipeline;

public sealed class NotifyStage(
    IEventPublisher publisher,
    ILogger<NotifyStage> logger)
{
    public async Task ExecuteAsync(
        UpsertResult upsertResult,
        ImportProgress progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(upsertResult);

        if (upsertResult.TotalCount == 0)
        {
            return;
        }

        var events = new List<ContentImportedEvent>(upsertResult.TotalCount);

        foreach (var item in upsertResult.NewItems)
        {
            events.Add(ToEvent(item, isNew: true));
        }

        foreach (var item in upsertResult.UpdatedItems)
        {
            events.Add(ToEvent(item, isNew: false));
        }

        using var activity = ImportActivitySource.Instance.StartActivity("Import.Notify");
        activity?.SetTag("import.event_count", events.Count);

        await publisher.PublishManyAsync(events, cancellationToken);

        progress.AddNotified(events.Count);

        logger.LogInformation(
            "Published {EventCount} ContentImportedEvent(s) for job {JobId}",
            events.Count, progress.JobId);
    }

    private static ContentImportedEvent ToEvent(ContentItem item, bool isNew) =>
        new()
        {
            ContentId = item.Id,
            ExternalId = item.ExternalId,
            SourceSystem = item.SourceSystem,
            Type = item.Type,
            Title = item.Title,
            Slug = item.Slug,
            ImportedAt = item.ImportedAt,
            IsNew = isNew,
        };
}
