using CmsImporter.Core.DTOs;
using CmsImporter.Core.Entities;

namespace CmsImporter.Core.Abstractions;

public interface IContentRepository
{
    IQueryable<ContentItem> Query();

    Task<ContentItem?> FindByExternalIdAsync(
        string sourceSystem,
        string externalId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<string, ContentItem>> FindByExternalIdsAsync(
        string sourceSystem,
        IReadOnlyCollection<string> externalIds,
        CancellationToken cancellationToken = default);

    Task<UpsertResult> UpsertManyAsync(
        IReadOnlyCollection<ContentItem> items,
        CancellationToken cancellationToken = default);
}
