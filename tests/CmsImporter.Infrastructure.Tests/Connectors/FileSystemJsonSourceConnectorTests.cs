using CmsImporter.Core.Abstractions;
using CmsImporter.Core.DTOs;
using CmsImporter.Infrastructure.Connectors;

using Microsoft.Extensions.Logging.Abstractions;

namespace CmsImporter.Infrastructure.Tests.Connectors;

internal static class AsyncEnumerableTestExtensions
{
    public static async Task<List<T>> ToListAsync<T>(
        this IAsyncEnumerable<T> source,
        CancellationToken ct = default)
    {
        var list = new List<T>();
        await foreach (var item in source.WithCancellation(ct))
        {
            list.Add(item);
        }

        return list;
    }
}

[TestFixture]
public sealed class FileSystemJsonSourceConnectorTests
{
    private string _tempDir = null!;

    private FileSystemJsonSourceConnector _connector = null!;

    [SetUp]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "cms-importer-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _connector = new FileSystemJsonSourceConnector(NullLogger<FileSystemJsonSourceConnector>.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    [Test]
    public async Task ReadAsync_StreamsItemsFromJsonArray()
    {
        WriteFile("export.json", """
            [
              { "externalId": "a-1", "type": "Page",    "title": "A", "bodyFormat": "md", "bodyRaw": "..." },
              { "externalId": "a-2", "type": "Article", "title": "B", "bodyFormat": "md", "bodyRaw": "..." }
            ]
            """);

        var items = await _connector.ReadAsync(MakeOptions(_tempDir, sourceSystem: "demo"))
            .ToListAsync();

        Assert.Multiple(() =>
        {
            Assert.That(items, Has.Count.EqualTo(2));
            Assert.That(items[0].ExternalId, Is.EqualTo("a-1"));
            Assert.That(items[1].Title, Is.EqualTo("B"));
            Assert.That(items.Select(i => i.SourceSystem), Is.All.EqualTo("demo"),
                "SourceSystem should be injected from connector options, not the JSON.");
        });
    }

    [Test]
    public async Task ReadAsync_ReadsAcrossMultipleFiles()
    {
        WriteFile("a.json", """[{ "externalId": "a", "type": "Page", "title": "A", "bodyFormat": "md", "bodyRaw": "" }]""");
        WriteFile("b.json", """[{ "externalId": "b", "type": "Page", "title": "B", "bodyFormat": "md", "bodyRaw": "" }]""");

        var items = await _connector.ReadAsync(MakeOptions(_tempDir, sourceSystem: "multi"))
            .ToListAsync();

        Assert.That(items.Select(i => i.ExternalId), Is.EquivalentTo(new[] { "a", "b" }));
    }

    [Test]
    public async Task ReadAsync_HonorsPatternFilter()
    {
        WriteFile("keep.json", """[{ "externalId": "k", "type": "Page", "title": "K", "bodyFormat": "md", "bodyRaw": "" }]""");
        WriteFile("skip.txt", "not json");

        var items = await _connector.ReadAsync(
                MakeOptions(_tempDir, sourceSystem: "filter", pattern: "*.json"))
            .ToListAsync();

        Assert.That(items, Has.Count.EqualTo(1));
        Assert.That(items[0].ExternalId, Is.EqualTo("k"));
    }

    [Test]
    public void ReadAsync_MissingDirectory_Throws()
    {
        var missing = Path.Combine(_tempDir, "does-not-exist");

        Assert.That(
            async () => await _connector.ReadAsync(MakeOptions(missing, sourceSystem: "x")).ToListAsync(),
            Throws.InstanceOf<DirectoryNotFoundException>());
    }

    [Test]
    public void ReadAsync_MissingSourceSystemOption_Throws()
    {
        var options = new SourceConnectorOptions
        {
            Settings = new Dictionary<string, string> { ["path"] = _tempDir },
        };

        Assert.That(
            async () => await _connector.ReadAsync(options).ToListAsync(),
            Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public void ReadAsync_RespectsCancellation()
    {
        WriteFile("a.json", """[{ "externalId": "a", "type": "Page", "title": "A", "bodyFormat": "md", "bodyRaw": "" }]""");

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.That(
            async () => await _connector.ReadAsync(MakeOptions(_tempDir, sourceSystem: "x"), cts.Token).ToListAsync(),
            Throws.InstanceOf<OperationCanceledException>());
    }

    private void WriteFile(string name, string content) =>
        File.WriteAllText(Path.Combine(_tempDir, name), content);

    private static SourceConnectorOptions MakeOptions(
        string path, string sourceSystem, string? pattern = null)
    {
        var settings = new Dictionary<string, string>
        {
            ["path"] = path,
            ["sourceSystem"] = sourceSystem,
        };

        if (pattern is not null)
        {
            settings["pattern"] = pattern;
        }

        return new SourceConnectorOptions { Settings = settings };
    }
}
