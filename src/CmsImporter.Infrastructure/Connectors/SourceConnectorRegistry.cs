using CmsImporter.Application.Abstractions;
using CmsImporter.Core.Abstractions;

namespace CmsImporter.Infrastructure.Connectors;

public sealed class SourceConnectorRegistry(IEnumerable<ISourceConnector> connectors) : ISourceConnectorRegistry
{
    private readonly IReadOnlyDictionary<string, ISourceConnector> _byName =
        connectors.ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);

    public ISourceConnector Resolve(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return _byName.TryGetValue(name, out var connector)
            ? connector
            : throw new InvalidOperationException(
                $"No source connector registered with name '{name}'. Available: {string.Join(", ", _byName.Keys)}.");
    }

    public IReadOnlyList<string> AvailableConnectors => _byName.Keys.ToArray();
}
