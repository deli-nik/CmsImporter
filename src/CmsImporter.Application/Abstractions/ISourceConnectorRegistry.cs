using CmsImporter.Core.Abstractions;

namespace CmsImporter.Application.Abstractions;

/// <summary>
/// Resolves an <see cref="ISourceConnector"/> by its <see cref="ISourceConnector.Name"/>. The
/// registry is what makes adding a new source CMS purely additive — register a new
/// <see cref="ISourceConnector"/> implementation and it shows up here automatically.
/// </summary>
public interface ISourceConnectorRegistry
{
    /// <summary>Looks up a connector by name.</summary>
    /// <exception cref="InvalidOperationException">Thrown when no connector is registered under
    /// that name; the message lists the available alternatives.</exception>
    ISourceConnector Resolve(string name);

    /// <summary>The names of every registered connector — useful for the
    /// <c>GET /imports/connectors</c> endpoint and error messages.</summary>
    IReadOnlyList<string> AvailableConnectors { get; }
}
