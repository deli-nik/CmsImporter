using CmsImporter.Core.Entities;

namespace CmsImporter.WebApi.Models;

public sealed record ContentResponse
{
    public required Guid Id { get; init; }

    public required string ExternalId { get; init; }

    public required string SourceSystem { get; init; }

    public required string Type { get; init; }

    public required string Title { get; init; }

    public required string Slug { get; init; }

    public string? Author { get; init; }

    public DateTimeOffset? PublishedAt { get; init; }

    public required DateTimeOffset ImportedAt { get; init; }

    public required uint Version { get; init; }

    public IReadOnlyDictionary<string, string> Metadata { get; init; } =
        new Dictionary<string, string>();

    public static ContentResponse From(ContentItem item) =>
        new()
        {
            Id = item.Id,
            ExternalId = item.ExternalId,
            SourceSystem = item.SourceSystem,
            Type = item.Type.ToString(),
            Title = item.Title,
            Slug = item.Slug,
            Author = item.Author,
            PublishedAt = item.PublishedAt,
            ImportedAt = item.ImportedAt,
            Version = item.Version,
            Metadata = item.Metadata,
        };
}
