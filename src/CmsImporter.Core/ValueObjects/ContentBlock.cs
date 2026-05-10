namespace CmsImporter.Core.ValueObjects;

/// <summary>
/// One structural block within a <see cref="ContentBody"/> — e.g., a heading, paragraph, or list.
/// Lets the importer preserve structured content from CMSs that emit block-based JSON
/// (Gutenberg, Sanity, Strapi). Persisted inside the <c>body</c> JSONB column.
/// </summary>
public sealed record ContentBlock
{
    /// <summary>Block kind — e.g., "heading", "paragraph", "list".</summary>
    public required string Type { get; init; }

    /// <summary>Inline text content for the block.</summary>
    public required string Content { get; init; }

    /// <summary>Optional block-specific attributes (e.g., heading level, list style).</summary>
    public IReadOnlyDictionary<string, string>? Attributes { get; init; }
}
