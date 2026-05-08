using CmsImporter.Domain.ValueObjects;

namespace CmsImporter.Domain.Entities;

public sealed class ContentItem
{
    public Guid Id { get; private set; }
    public string ExternalId { get; private set; } = null!;
    public string SourceSystem { get; private set; } = null!;
    public ContentType Type { get; private set; }
    public string Title { get; private set; } = null!;
    public string Slug { get; private set; } = null!;
    public string? Author { get; private set; }
    public DateTimeOffset? PublishedAt { get; private set; }
    public ContentBody Body { get; private set; } = null!;
    public IReadOnlyDictionary<string, string> Metadata { get; private set; } =
        new Dictionary<string, string>();
    public DateTimeOffset ImportedAt { get; private set; }
    public uint Version { get; private set; }

    private ContentItem()
    {
    }

    public static ContentItem Import(
        string externalId,
        string sourceSystem,
        ContentType type,
        string title,
        string slug,
        string? author,
        DateTimeOffset? publishedAt,
        ContentBody body,
        IReadOnlyDictionary<string, string> metadata,
        DateTimeOffset importedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(externalId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceSystem);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        ArgumentNullException.ThrowIfNull(body);
        ArgumentNullException.ThrowIfNull(metadata);

        return new ContentItem
        {
            Id = Guid.NewGuid(),
            ExternalId = externalId,
            SourceSystem = sourceSystem,
            Type = type,
            Title = title,
            Slug = slug,
            Author = author,
            PublishedAt = publishedAt,
            Body = body,
            Metadata = metadata,
            ImportedAt = importedAt,
            Version = 1,
        };
    }

    public void ApplyUpdate(
        ContentType type,
        string title,
        string slug,
        string? author,
        DateTimeOffset? publishedAt,
        ContentBody body,
        IReadOnlyDictionary<string, string> metadata,
        DateTimeOffset importedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        ArgumentNullException.ThrowIfNull(body);
        ArgumentNullException.ThrowIfNull(metadata);

        Type = type;
        Title = title;
        Slug = slug;
        Author = author;
        PublishedAt = publishedAt;
        Body = body;
        Metadata = metadata;
        ImportedAt = importedAt;
        Version++;
    }
}
