using CmsImporter.Application.Abstractions;
using CmsImporter.Core.Abstractions;

namespace CmsImporter.Infrastructure.Connectors;

/// <summary>
/// Default <see cref="ISourceConnectorRegistry"/> — receives every registered
/// <see cref="ISourceConnector"/> via DI and indexes them by name (case-insensitive).
/// Adding a new source CMS is purely a matter of registering its connector; the registry
/// picks it up automatically.
/// </summary>
public sealed class SourceConnectorRegistry(IEnumerable<ISourceConnector> connectors) : ISourceConnectorRegistry
{
    private readonly IReadOnlyDictionary<string, ISourceConnector> _byName =
        connectors.ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public ISourceConnector Resolve(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return _byName.TryGetValue(name, out var connector)
            ? connector
            : throw new InvalidOperationException(
                $"No source connector registered with name '{name}'. Available: {string.Join(", ", _byName.Keys)}.");
    }

    /// <inheritdoc />
    public IReadOnlyList<string> AvailableConnectors => _byName.Keys.ToArray();
}
