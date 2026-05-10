namespace CmsImporter.Core.DTOs;

/// <summary>
/// The shape returned by an <see cref="Abstractions.ISourceConnector"/> — a content item as it
/// arrives from the originating CMS, before transform/validate/persist. Source-shape, not
/// domain-shape: optional fields, raw type strings, no internal id.
/// </summary>
public sealed record RawContent
{
    /// <summary>Identifier in the source CMS.</summary>
    public required string ExternalId { get; init; }

    /// <summary>Logical source CMS name. Connectors typically inject this from their options.</summary>
    public required string SourceSystem { get; init; }

    /// <summary>Source-system type string (e.g., "Page", "post", "article"). Mapped to a domain
    /// <see cref="Entities.ContentType"/> by the transform stage.</summary>
    public required string Type { get; init; }

    /// <summary>Title.</summary>
    public required string Title { get; init; }

    /// <summary>Optional slug — synthesised from <see cref="Title"/> if missing.</summary>
    public string? Slug { get; init; }

    /// <summary>Optional author / byline.</summary>
    public string? Author { get; init; }

    /// <summary>Original publish timestamp from the source CMS, if known.</summary>
    public DateTimeOffset? PublishedAt { get; init; }

    /// <summary>Format of the raw body (e.g., "markdown", "text/html").</summary>
    public required string BodyFormat { get; init; }

    /// <summary>The body payload in <see cref="BodyFormat"/>.</summary>
    public required string BodyRaw { get; init; }

    /// <summary>Optional structured block list parallel to the raw body.</summary>
    public IReadOnlyList<RawContentBlock>? BodyBlocks { get; init; }

    /// <summary>Free-form metadata key/value pairs from the source.</summary>
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}

/// <summary>One structural block from a source CMS — mapped 1:1 to a
/// <see cref="ValueObjects.ContentBlock"/> by the transform stage.</summary>
public sealed record RawContentBlock
{
    /// <summary>Block kind.</summary>
    public required string Type { get; init; }

    /// <summary>Block text content.</summary>
    public required string Content { get; init; }

    /// <summary>Optional block-specific attributes.</summary>
    public IReadOnlyDictionary<string, string>? Attributes { get; init; }
}
