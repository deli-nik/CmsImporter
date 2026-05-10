namespace CmsImporter.WebApi.Models;

/// <summary>Request body for <c>POST /imports</c>.</summary>
public sealed record CreateImportRequest
{
    /// <summary>Name of the source connector to use (e.g., <c>"FileSystem"</c>, <c>"HttpRest"</c>).
    /// Resolved against <c>ISourceConnectorRegistry</c>.</summary>
    public required string Source { get; init; }

    /// <summary>Connector-specific key/value settings (paths, URLs, source-system tag, ...).  
    /// The connector validates the keys it requires at runtime.</summary>
    public IReadOnlyDictionary<string, string>? Config { get; init; }
}
