using CmsImporter.Core.Abstractions;
using CmsImporter.Core.Entities;

using Microsoft.EntityFrameworkCore;

#pragma warning disable CA1304, CA1311 // Provider-translated ToLower() is intentional for case-insensitive search.

namespace CmsImporter.Application.Queries;

/// <summary>
/// Read-side service over <see cref="IContentRepository.Query"/>. Demonstrates
/// <see cref="IQueryable{T}"/> composition: each predicate is appended to the expression tree
/// only when the corresponding criterion is present, then materialised via <c>ToListAsync</c>.
/// EF Core translates the whole chain to a single SQL statement; predicates that aren't applied
/// don't appear in the WHERE clause.
/// </summary>
public sealed class ContentQueryService(IContentRepository repository)
{
    /// <summary>
    /// Builds the filter chain from <paramref name="criteria"/> and returns the matching items,
    /// newest first, with paging applied.
    /// </summary>
    public async Task<IReadOnlyList<ContentItem>> SearchAsync(
        ContentSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        IQueryable<ContentItem> query = repository.Query();

        if (!string.IsNullOrWhiteSpace(criteria.SourceSystem))
        {
            query = query.Where(c => c.SourceSystem == criteria.SourceSystem);
        }

        if (criteria.Type is { } type)
        {
            query = query.Where(c => c.Type == type);
        }

        if (criteria.SinceImportedAt is { } since)
        {
            query = query.Where(c => c.ImportedAt >= since);
        }

        if (!string.IsNullOrWhiteSpace(criteria.TitleContains))
        {
            var needle = criteria.TitleContains.ToLowerInvariant();
            query = query.Where(c => c.Title.ToLower().Contains(needle));
        }

        var limit = Math.Clamp(criteria.Limit, 1, 500);
        var offset = Math.Max(0, criteria.Offset);

        return await query
            .OrderByDescending(c => c.ImportedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    /// <summary>Returns the count for the same filter chain (without paging).</summary>
    public Task<int> CountAsync(ContentSearchCriteria criteria, CancellationToken cancellationToken)
    {
        IQueryable<ContentItem> query = repository.Query();

        if (!string.IsNullOrWhiteSpace(criteria.SourceSystem))
        {
            query = query.Where(c => c.SourceSystem == criteria.SourceSystem);
        }

        if (criteria.Type is { } type)
        {
            query = query.Where(c => c.Type == type);
        }

        return query.CountAsync(cancellationToken);
    }
}
