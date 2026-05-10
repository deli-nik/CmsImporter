using CmsImporter.Core.Entities;

namespace CmsImporter.WebApi.Models;

/// <summary>
/// API response DTO for a single imported content item. Constructed from a
/// <see cref="ContentItem"/> via <see cref="From"/>.
/// </summary>
public sealed record ContentResponse
{
    /// <summary>Internal primary key assigned by the importer.</summary>
    public required Guid Id { get; init; }

    /// <summary>Identifier of the item in the originating CMS.</summary>
    public required string ExternalId { get; init; }

    /// <summary>Logical source CMS name (e.g., "wordpress-blog").</summary>
    public required string SourceSystem { get; init; }

    /// <summary>Content type as a human-readable string (e.g., "Page", "Article").</summary>
    public required string Type { get; init; }

    /// <summary>Human-readable title.</summary>
    public required string Title { get; init; }

    /// <summary>URL-friendly slug.</summary>
    public required string Slug { get; init; }

    /// <summary>Optional author/byline.</summary>
    public string? Author { get; init; }

    /// <summary>Original publish timestamp from the source CMS, if known.</summary>
    public DateTimeOffset? PublishedAt { get; init; }

    /// <summary>UTC instant the importer last wrote this item.</summary>
    public required DateTimeOffset ImportedAt { get; init; }

    /// <summary>Optimistic-concurrency version counter; incremented on every update.</summary>
    public required uint Version { get; init; }

    /// <summary>Free-form key/value metadata carried over from the source CMS.</summary>
    public IReadOnlyDictionary<string, string> Metadata { get; init; } =
        new Dictionary<string, string>();

    /// <summary>Projects a <see cref="ContentItem"/> domain entity into a <see cref="ContentResponse"/>.</summary>
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
