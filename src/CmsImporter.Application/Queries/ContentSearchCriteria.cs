using CmsImporter.Core.Entities;

namespace CmsImporter.Application.Queries;

public sealed record ContentSearchCriteria
{
    public string? SourceSystem { get; init; }

    public ContentType? Type { get; init; }

    public DateTimeOffset? SinceImportedAt { get; init; }

    public string? TitleContains { get; init; }

    public int Limit { get; init; } = 50;

    public int Offset { get; init; }
}
