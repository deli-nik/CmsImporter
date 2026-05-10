using CmsImporter.Core.Entities;

namespace CmsImporter.Core.Events;

/// <summary>
/// The domain event published to upstream systems when a content item lands in the importer's
/// store. One event is emitted per upserted item; the routing key is
/// <c>cms.content.imported.{sourceSystem}.{type}</c> on the <c>cms.content</c> topic exchange.
/// </summary>
public sealed record ContentImportedEvent
{
    /// <summary>The importer's internal id of the upserted item.</summary>
    public required Guid ContentId { get; init; }

    /// <summary>Identifier of the item in the originating CMS.</summary>
    public required string ExternalId { get; init; }

    /// <summary>Logical source CMS name.</summary>
    public required string SourceSystem { get; init; }

    /// <summary>The content kind.</summary>
    public required ContentType Type { get; init; }

    /// <summary>Title at the time of this import.</summary>
    public required string Title { get; init; }

    /// <summary>URL-friendly slug at the time of this import.</summary>
    public required string Slug { get; init; }

    /// <summary>UTC instant the importer wrote this row.</summary>
    public required DateTimeOffset ImportedAt { get; init; }

    /// <summary>
    /// <see langword="true"/> when this is the first time the importer has seen this
    /// <c>(SourceSystem, ExternalId)</c>; <see langword="false"/> when it's an update of an
    /// existing item. Subscribers can route create vs update flows accordingly.
    /// </summary>
    public required bool IsNew { get; init; }
}
