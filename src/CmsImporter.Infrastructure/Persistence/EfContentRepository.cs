using CmsImporter.Core.Abstractions;
using CmsImporter.Core.DTOs;
using CmsImporter.Core.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CmsImporter.Infrastructure.Persistence;

public sealed class EfContentRepository(
    AppDbContext db,
    TimeProvider timeProvider,
    ILogger<EfContentRepository> logger) : IContentRepository
{
    public IQueryable<ContentItem> Query() => db.ContentItems.AsNoTracking();

    public Task<ContentItem?> FindByExternalIdAsync(
        string sourceSystem,
        string externalId,
        CancellationToken cancellationToken = default) =>
        db.ContentItems
            .AsNoTracking()
            .FirstOrDefaultAsync(
                c => c.SourceSystem == sourceSystem && c.ExternalId == externalId,
                cancellationToken);

    public async Task<IReadOnlyDictionary<string, ContentItem>> FindByExternalIdsAsync(
        string sourceSystem,
        IReadOnlyCollection<string> externalIds,
        CancellationToken cancellationToken = default)
    {
        if (externalIds.Count == 0)
        {
            return new Dictionary<string, ContentItem>();
        }

        var ids = externalIds.Distinct().ToArray();

        var items = await db.ContentItems
            .AsNoTracking()
            .Where(c => c.SourceSystem == sourceSystem && ids.Contains(c.ExternalId))
            .ToListAsync(cancellationToken);

        return items.ToDictionary(c => c.ExternalId);
    }

    public async Task<UpsertResult> UpsertManyAsync(
        IReadOnlyCollection<ContentItem> items,
        CancellationToken cancellationToken = default)
    {
        if (items.Count == 0)
        {
            return UpsertResult.Empty;
        }

        var newItems = new List<ContentItem>();
        var updatedItems = new List<ContentItem>();
        var importedAt = timeProvider.GetUtcNow();

        // Disable auto-detect for the entire operation:
        // - Prevents implicit DetectChanges sweeps during tracked queries in the loop.
        // - One explicit DetectChanges sweep before save.
        // - Clear the tracker after save so the captured graph is GC-eligible.
        var previousAutoDetect = db.ChangeTracker.AutoDetectChangesEnabled;
        db.ChangeTracker.AutoDetectChangesEnabled = false;
        try
        {
            foreach (var sourceGroup in items.GroupBy(i => i.SourceSystem))
            {
                var externalIds = sourceGroup.Select(i => i.ExternalId).Distinct().ToArray();

                // Tracked load: we need EF to follow updates to existing rows.
                var existing = await db.ContentItems
                    .Where(c => c.SourceSystem == sourceGroup.Key && externalIds.Contains(c.ExternalId))
                    .ToDictionaryAsync(c => c.ExternalId, cancellationToken);

                foreach (var candidate in sourceGroup)
                {
                    if (existing.TryGetValue(candidate.ExternalId, out var current))
                    {
                        current.Type = candidate.Type;
                        current.Title = candidate.Title;
                        current.Slug = candidate.Slug;
                        current.Author = candidate.Author;
                        current.PublishedAt = candidate.PublishedAt;
                        current.Body = candidate.Body;
                        current.Metadata = candidate.Metadata;
                        current.ImportedAt = importedAt;
                        current.Version++;
                        updatedItems.Add(current);
                    }
                    else
                    {
                        candidate.Id = candidate.Id == Guid.Empty ? Guid.NewGuid() : candidate.Id;
                        candidate.Version = 1;
                        candidate.ImportedAt = importedAt;
                        db.ContentItems.Add(candidate);
                        newItems.Add(candidate);
                    }
                }
            }

            db.ChangeTracker.DetectChanges();
            await db.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            db.ChangeTracker.AutoDetectChangesEnabled = previousAutoDetect;
            db.ChangeTracker.Clear();
        }

        logger.LogDebug(
            "Upserted {NewCount} new + {UpdatedCount} updated content items",
            newItems.Count, updatedItems.Count);

        return new UpsertResult { NewItems = newItems, UpdatedItems = updatedItems };
    }
}
