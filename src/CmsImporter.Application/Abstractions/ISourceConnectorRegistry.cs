using CmsImporter.Core.Abstractions;

namespace CmsImporter.Application.Abstractions;

public interface ISourceConnectorRegistry
{
    ISourceConnector Resolve(string name);

    IReadOnlyList<string> AvailableConnectors { get; }
}
