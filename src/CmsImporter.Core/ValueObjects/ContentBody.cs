namespace CmsImporter.Core.ValueObjects;

/// <summary>
/// The body of a <see cref="Entities.ContentItem"/>. Carries both a raw representation (for
/// fidelity / fallback) and an optional structured block list (for CMSs that publish
/// well-typed content trees). Persisted as a JSONB column.
/// </summary>
public sealed record ContentBody
{
    /// <summary>MIME type or format identifier of the raw payload — e.g., "markdown", "text/html".</summary>
    public required string Format { get; init; }

    /// <summary>Source-of-truth raw payload in the declared <see cref="Format"/>.</summary>
    public required string Raw { get; init; }

    /// <summary>Optional structured block list parallel to <see cref="Raw"/>.</summary>
    public IReadOnlyList<ContentBlock>? Blocks { get; init; }

    /// <summary>Returns an empty body — convenient default for entities and tests.</summary>
    public static ContentBody Empty(string format = "text/plain") =>
        new() { Format = format, Raw = string.Empty, Blocks = null };
}
