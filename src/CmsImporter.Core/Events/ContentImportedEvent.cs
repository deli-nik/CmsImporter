using CmsImporter.Core.Entities;

namespace CmsImporter.Core.Events;

public sealed record ContentImportedEvent
{
    public required Guid ContentId { get; init; }

    public required string ExternalId { get; init; }

    public required string SourceSystem { get; init; }

    public required ContentType Type { get; init; }

    public required string Title { get; init; }

    public required string Slug { get; init; }

    public required DateTimeOffset ImportedAt { get; init; }

    public required bool IsNew { get; init; }
}
