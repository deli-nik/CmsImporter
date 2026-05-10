using System.Runtime.CompilerServices;

using CmsImporter.Core.Abstractions;
using CmsImporter.Core.DTOs;
using CmsImporter.Infrastructure.Connectors;

namespace CmsImporter.Infrastructure.Tests.Connectors;

[TestFixture]
public sealed class SourceConnectorRegistryTests
{
    [Test]
    public void Resolve_KnownName_ReturnsConnector()
    {
        var fs = new StubConnector("FileSystem");
        var http = new StubConnector("HttpRest");
        var registry = new SourceConnectorRegistry([fs, http]);

        Assert.That(registry.Resolve("FileSystem"), Is.SameAs(fs));
        Assert.That(registry.Resolve("HttpRest"), Is.SameAs(http));
    }

    [Test]
    public void Resolve_IsCaseInsensitive()
    {
        var fs = new StubConnector("FileSystem");
        var registry = new SourceConnectorRegistry([fs]);

        Assert.That(registry.Resolve("filesystem"), Is.SameAs(fs));
        Assert.That(registry.Resolve("FILESYSTEM"), Is.SameAs(fs));
    }

    [Test]
    public void Resolve_UnknownName_ThrowsWithAvailableList()
    {
        var registry = new SourceConnectorRegistry([new StubConnector("FileSystem"), new StubConnector("HttpRest")]);

        var ex = Assert.Throws<InvalidOperationException>(() => registry.Resolve("Mystery"));
        Assert.That(ex!.Message, Does.Contain("FileSystem").And.Contain("HttpRest"));
    }

    [Test]
    public void AvailableConnectors_ListsAllNames()
    {
        var registry = new SourceConnectorRegistry([new StubConnector("FileSystem"), new StubConnector("HttpRest")]);

        Assert.That(registry.AvailableConnectors, Is.EquivalentTo(new[] { "FileSystem", "HttpRest" }));
    }

    [Test]
    public void Resolve_NullOrWhitespace_Throws()
    {
        var registry = new SourceConnectorRegistry([new StubConnector("X")]);

        Assert.That(() => registry.Resolve(null!), Throws.InstanceOf<ArgumentException>());
        Assert.That(() => registry.Resolve("   "), Throws.InstanceOf<ArgumentException>());
    }

    private sealed class StubConnector(string name) : ISourceConnector
    {
        public string Name { get; } = name;

        public async IAsyncEnumerable<RawContent> ReadAsync(
            SourceConnectorOptions options,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            yield break;
        }
    }
}
