using CmsImporter.Core.Entities;

namespace CmsImporter.Core.DTOs;

/// <summary>
/// The outcome of <see cref="Abstractions.IContentRepository.UpsertManyAsync"/>. Returned items
/// are partitioned so the notify stage can emit <c>IsNew=true</c> events for inserts and
/// <c>IsNew=false</c> for updates without a second round trip to the database.
/// </summary>
public sealed record UpsertResult
{
    /// <summary>Items that were inserted (didn't previously exist for this source/external id).</summary>
    public required IReadOnlyList<ContentItem> NewItems { get; init; }

    /// <summary>Items that were updated in place (existing rows, fields refreshed).</summary>
    public required IReadOnlyList<ContentItem> UpdatedItems { get; init; }

    /// <summary>Sum of <see cref="NewItems"/> and <see cref="UpdatedItems"/> counts.</summary>
    public int TotalCount => NewItems.Count + UpdatedItems.Count;

    /// <summary>The empty result — used as the no-op return when an upsert batch is empty.</summary>
    public static UpsertResult Empty { get; } =
        new() { NewItems = [], UpdatedItems = [] };
}
