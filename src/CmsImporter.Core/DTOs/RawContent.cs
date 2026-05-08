namespace CmsImporter.Core.DTOs;

public sealed record RawContent
{
    public required string ExternalId { get; init; }

    public required string SourceSystem { get; init; }

    public required string Type { get; init; }

    public required string Title { get; init; }

    public string? Slug { get; init; }

    public string? Author { get; init; }

    public DateTimeOffset? PublishedAt { get; init; }

    public required string BodyFormat { get; init; }

    public required string BodyRaw { get; init; }

    public IReadOnlyList<RawContentBlock>? BodyBlocks { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}

public sealed record RawContentBlock
{
    public required string Type { get; init; }

    public required string Content { get; init; }

    public IReadOnlyDictionary<string, string>? Attributes { get; init; }
}
