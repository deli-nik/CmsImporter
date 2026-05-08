using CmsImporter.Core.Entities;

namespace CmsImporter.Core.DTOs;

public sealed record UpsertResult
{
    public required IReadOnlyList<ContentItem> NewItems { get; init; }

    public required IReadOnlyList<ContentItem> UpdatedItems { get; init; }

    public int TotalCount => NewItems.Count + UpdatedItems.Count;

    public static UpsertResult Empty { get; } =
        new() { NewItems = [], UpdatedItems = [] };
}
