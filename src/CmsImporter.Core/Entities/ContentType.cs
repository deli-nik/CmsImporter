namespace CmsImporter.Core.Entities;

/// <summary>
/// The kind of content represented by a <see cref="ContentItem"/>. Persisted as a string column
/// in PostgreSQL for human-readable queries via <c>psql</c>.
/// </summary>
public enum ContentType
{
    /// <summary>The source-system type string did not match any known kind.</summary>
    Unknown = 0,

    /// <summary>A general web page (e.g., "Home", "Pricing", "About").</summary>
    Page = 1,

    /// <summary>An article or blog post — typically dated, attributed to an author.</summary>
    Article = 2,

    /// <summary>A media asset (image, video, document) referenced by URL or embedded.</summary>
    Media = 3,
}
