using CmsImporter.Core.Abstractions;

namespace CmsImporter.Application.Pipeline;

public sealed record ImportJob
{
    public required Guid Id { get; init; }

    public required string SourceConnector { get; init; }

    public required SourceConnectorOptions Options { get; init; }

    public required DateTimeOffset EnqueuedAt { get; init; }
}
