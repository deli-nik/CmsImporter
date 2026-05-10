using CmsImporter.Core.DTOs;
using CmsImporter.Core.Entities;

namespace CmsImporter.Core.Abstractions;

/// <summary>
/// The persistence boundary for <see cref="ContentItem"/>. Splits read-style access (composable
/// <see cref="IQueryable{T}"/>) from write-style access (batched upsert) so the application
/// layer can build expression trees for queries without leaking ORM details into domain code.
/// </summary>
public interface IContentRepository
{
    /// <summary>
    /// Returns a no-tracking <see cref="IQueryable{T}"/> over <see cref="ContentItem"/>. The
    /// caller composes filters with deferred execution, then materialises (e.g.,
    /// <c>ToListAsync</c>) when ready. Used by <c>ContentQueryService</c> for the read path.
    /// </summary>
    IQueryable<ContentItem> Query();

    /// <summary>Looks up a single item by its source-system natural key.</summary>
    Task<ContentItem?> FindByExternalIdAsync(
        string sourceSystem,
        string externalId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Batched lookup — one round trip resolves N candidates against the store. Returned
    /// dictionary is keyed on <c>ExternalId</c>.
    /// </summary>
    Task<IReadOnlyDictionary<string, ContentItem>> FindByExternalIdsAsync(
        string sourceSystem,
        IReadOnlyCollection<string> externalIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts new items and updates existing ones in a single transaction. Returns the
    /// partitioned result so the caller can distinguish creates from updates without re-querying.
    /// </summary>
    Task<UpsertResult> UpsertManyAsync(
        IReadOnlyCollection<ContentItem> items,
        CancellationToken cancellationToken = default);
}
