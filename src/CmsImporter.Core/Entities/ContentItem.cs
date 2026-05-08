using CmsImporter.Core.ValueObjects;

namespace CmsImporter.Core.Entities;

public sealed class ContentItem
{
    public Guid Id { get; set; }

    public string ExternalId { get; set; } = string.Empty;

    public string SourceSystem { get; set; } = string.Empty;

    public ContentType Type { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string? Author { get; set; }

    public DateTimeOffset? PublishedAt { get; set; }

    public ContentBody Body { get; set; } = ContentBody.Empty();

    public IReadOnlyDictionary<string, string> Metadata { get; set; } =
        new Dictionary<string, string>();

    public DateTimeOffset ImportedAt { get; set; }

    public uint Version { get; set; }
}
