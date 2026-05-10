using CmsImporter.Core.Abstractions;

namespace CmsImporter.Application.Pipeline;

/// <summary>
/// An import request waiting (or about to wait) on the worker channel. Created by the API
/// endpoint, drained by the <c>ImportWorker</c> background service.
/// </summary>
public sealed record ImportJob
{
    /// <summary>Unique identifier; surfaced back to the caller in the <c>POST /imports</c> response.</summary>
    public required Guid Id { get; init; }

    /// <summary>Name of the connector to use (e.g., "FileSystem", "HttpRest"). Resolved against
    /// <see cref="Abstractions.ISourceConnectorRegistry"/>.</summary>
    public required string SourceConnector { get; init; }

    /// <summary>Connector-specific configuration (paths, URLs, source-system tag, ...).</summary>
    public required SourceConnectorOptions Options { get; init; }

    /// <summary>UTC instant the job was accepted by the API.</summary>
    public required DateTimeOffset EnqueuedAt { get; init; }
}
