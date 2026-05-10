using CmsImporter.Core.Entities;

namespace CmsImporter.Application.Queries;

/// <summary>
/// Filter and paging options for the read-side <c>GET /content</c> endpoint. Consumed by
/// <see cref="ContentQueryService"/>, which composes filters into the <see cref="IQueryable{T}"/>
/// expression tree before materialising.
/// </summary>
public sealed record ContentSearchCriteria
{
    /// <summary>Restrict to a single source CMS, or <see langword="null"/> for any.</summary>
    public string? SourceSystem { get; init; }

    /// <summary>Restrict to a single content type, or <see langword="null"/> for any.</summary>
    public ContentType? Type { get; init; }

    /// <summary>Only return items imported at or after this UTC instant.</summary>
    public DateTimeOffset? SinceImportedAt { get; init; }

    /// <summary>Case-insensitive substring filter on <c>Title</c>.</summary>
    public string? TitleContains { get; init; }

    /// <summary>Maximum rows to return; clamped to <c>[1, 500]</c>.</summary>
    public int Limit { get; init; } = 50;

    /// <summary>Number of rows to skip — for paging.</summary>
    public int Offset { get; init; }
}
