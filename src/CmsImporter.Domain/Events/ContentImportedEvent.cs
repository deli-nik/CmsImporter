using CmsImporter.Domain.Entities;

namespace CmsImporter.Domain.Events;

public sealed record ContentImportedEvent(
    Guid ContentId,
    string ExternalId,
    string SourceSystem,
    ContentType Type,
    string Title,
    string Slug,
    DateTimeOffset ImportedAt,
    bool IsNew);
