using System.Net.Http.Json;
using System.Text.Json;

using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace CmsImporter.IntegrationTests;

[TestFixture]
[Category("Integration")]
public sealed class ImportPipelineIntegrationTests
{
    private const string RabbitUser = "cms";

    private const string RabbitPass = "cms";

    private PostgreSqlContainer _postgres = null!;

    private RabbitMqContainer _rabbitmq = null!;

    private CmsImporterTestFactory _factory = null!;

    private string _samplesPath = null!;

    private static readonly JsonSerializerOptions Json =
        new(JsonSerializerDefaults.Web);

    [OneTimeSetUp]
    public async Task SetUpAsync()
    {
        _postgres = new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase("cms_importer")
            .WithUsername("cms")
            .WithPassword("cms")
            .Build();

        _rabbitmq = new RabbitMqBuilder("rabbitmq:3.13-management-alpine")
            .WithUsername(RabbitUser)
            .WithPassword(RabbitPass)
            .Build();

        await Task.WhenAll(_postgres.StartAsync(), _rabbitmq.StartAsync());

        _samplesPath = Path.Combine(
            Path.GetTempPath(), "cms-importer-int-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_samplesPath);
        await File.WriteAllTextAsync(Path.Combine(_samplesPath, "test.json"), """
            [
              { "externalId": "int-1", "type": "Page",    "title": "Hi",    "bodyFormat": "md", "bodyRaw": "x", "metadata": { "category": "news" } },
              { "externalId": "int-2", "type": "Article", "title": "Hello", "bodyFormat": "md", "bodyRaw": "y" },
              { "externalId": "int-3", "type": "Article", "title": "World", "bodyFormat": "md", "bodyRaw": "z", "metadata": { "category": "blog" } }
            ]
            """);

        _factory = new CmsImporterTestFactory(
            postgresConnectionString: _postgres.GetConnectionString(),
            rabbitHost: _rabbitmq.Hostname,
            rabbitPort: _rabbitmq.GetMappedPublicPort(5672),
            rabbitUser: RabbitUser,
            rabbitPassword: RabbitPass);
    }

    [OneTimeTearDown]
    public async Task TearDownAsync()
    {
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
        await _rabbitmq.DisposeAsync();

        if (Directory.Exists(_samplesPath))
        {
            Directory.Delete(_samplesPath, recursive: true);
        }
    }

    [Test]
    public async Task FullImportFlow_PersistsItems_PublishesEvents_AndQueriesBack()
    {
        var sourceSystem = "test-flow-" + Guid.NewGuid().ToString("N")[..8];

        // Subscribe BEFORE triggering the import so no events are missed.
        await using var subscriber = await TestSubscriber.CreateAsync(
            host: _rabbitmq.Hostname,
            port: _rabbitmq.GetMappedPublicPort(5672),
            user: RabbitUser,
            pass: RabbitPass,
            exchange: "cms.content",
            routingKey: $"cms.content.imported.{sourceSystem}.#");

        var client = _factory.CreateClient();

        // Wait for /health = Healthy (covers DbContextCheck connecting to Postgres).
        await PollUntilAsync(
            async () => (await client.GetAsync("/health")).IsSuccessStatusCode,
            timeout: TimeSpan.FromSeconds(30),
            label: "/health Healthy");

        // POST /imports
        var enqueueResponse = await client.PostAsJsonAsync("/imports", new
        {
            source = "FileSystem",
            config = new Dictionary<string, string>
            {
                ["path"] = _samplesPath,
                ["sourceSystem"] = sourceSystem,
            },
        });
        Assert.That(enqueueResponse.IsSuccessStatusCode, Is.True,
            $"POST /imports failed: {enqueueResponse.StatusCode} {await enqueueResponse.Content.ReadAsStringAsync()}");

        var enqueue = await enqueueResponse.Content.ReadFromJsonAsync<EnqueueDto>(Json);
        Assert.That(enqueue, Is.Not.Null);

        // Poll job until Completed.
        ImportJobDto? final = null;
        await PollUntilAsync(
            async () =>
            {
                final = await client.GetFromJsonAsync<ImportJobDto>($"/imports/{enqueue!.JobId}", Json);
                return final?.Status is "Completed" or "Failed";
            },
            timeout: TimeSpan.FromSeconds(30),
            label: "import job completion");

        Assert.Multiple(() =>
        {
            Assert.That(final!.Status, Is.EqualTo("Completed"), final.FailureReason);
            Assert.That(final.Counts.Loaded, Is.EqualTo(3));
            Assert.That(final.Counts.New, Is.EqualTo(3));
            Assert.That(final.Counts.Notified, Is.EqualTo(3));
        });

        // Read back via /content (exercises IQueryable composition + JSONB Metadata round-trip).
        var content = await client.GetFromJsonAsync<List<ContentDto>>(
            $"/content?sourceSystem={sourceSystem}&limit=50", Json);
        Assert.That(content, Has.Count.EqualTo(3));
        Assert.That(content!.Select(c => c.ExternalId),
            Is.EquivalentTo(new[] { "int-1", "int-2", "int-3" }));
        Assert.That(content.First(c => c.ExternalId == "int-1").Metadata["category"], Is.EqualTo("news"));

        // Wait for events to settle on the bus.
        await PollUntilAsync(
            () => Task.FromResult(subscriber.Received.Count >= 3),
            timeout: TimeSpan.FromSeconds(10),
            label: "3 events received");

        Assert.That(subscriber.Received, Has.Count.EqualTo(3));
        Assert.That(
            subscriber.Received.Select(m => m.RoutingKey),
            Is.All.StartsWith($"cms.content.imported.{sourceSystem}."));
    }

    [Test]
    public async Task ReImport_SameItems_UpsertsAndEmitsIsNewFalse()
    {
        var sourceSystem = "test-reimport-" + Guid.NewGuid().ToString("N")[..8];

        await using var subscriber = await TestSubscriber.CreateAsync(
            host: _rabbitmq.Hostname,
            port: _rabbitmq.GetMappedPublicPort(5672),
            user: RabbitUser,
            pass: RabbitPass,
            exchange: "cms.content",
            routingKey: $"cms.content.imported.{sourceSystem}.#");

        var client = _factory.CreateClient();

        // First import.
        var first = await EnqueueAndWaitAsync(client, _samplesPath, sourceSystem);
        Assert.That(first.Counts.New, Is.EqualTo(3));

        await PollUntilAsync(
            () => Task.FromResult(subscriber.Received.Count >= 3),
            timeout: TimeSpan.FromSeconds(10),
            label: "first import events");
        var firstRunCount = subscriber.Received.Count;

        // Re-import the same data — same (sourceSystem, externalId) keys exist now.
        var second = await EnqueueAndWaitAsync(client, _samplesPath, sourceSystem);

        Assert.Multiple(() =>
        {
            Assert.That(second.Counts.Loaded, Is.EqualTo(3));
            Assert.That(second.Counts.New, Is.Zero, "second run should be 0 new");
            Assert.That(second.Counts.Updated, Is.EqualTo(3));
        });

        await PollUntilAsync(
            () => Task.FromResult(subscriber.Received.Count >= firstRunCount + 3),
            timeout: TimeSpan.FromSeconds(10),
            label: "second import events");

        // ConcurrentQueue iterates in FIFO order, so Skip(firstRunCount).Take(3) is the
        // second-run batch.
        var secondRunMessages = subscriber.Received
            .Skip(firstRunCount)
            .Take(3)
            .ToArray();

        Assert.That(secondRunMessages, Has.All.Matches<TestSubscriber.ReceivedMessage>(
            m => m.Body.Contains("\"isNew\":false", StringComparison.OrdinalIgnoreCase)));
    }

    private async Task<ImportJobDto> EnqueueAndWaitAsync(HttpClient client, string path, string sourceSystem)
    {
        var enqueueResponse = await client.PostAsJsonAsync("/imports", new
        {
            source = "FileSystem",
            config = new Dictionary<string, string>
            {
                ["path"] = path,
                ["sourceSystem"] = sourceSystem,
            },
        });
        enqueueResponse.EnsureSuccessStatusCode();
        var enqueue = await enqueueResponse.Content.ReadFromJsonAsync<EnqueueDto>(Json);

        ImportJobDto? final = null;
        await PollUntilAsync(
            async () =>
            {
                final = await client.GetFromJsonAsync<ImportJobDto>($"/imports/{enqueue!.JobId}", Json);
                return final?.Status is "Completed" or "Failed";
            },
            timeout: TimeSpan.FromSeconds(30),
            label: $"job {enqueue!.JobId} completion");

        Assert.That(final!.Status, Is.EqualTo("Completed"), final.FailureReason);
        return final;
    }

    private static async Task PollUntilAsync(
        Func<Task<bool>> condition, TimeSpan timeout, string label)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (await condition())
            {
                return;
            }

            await Task.Delay(150);
        }

        Assert.Fail($"Timed out waiting for: {label}");
    }

    private sealed record EnqueueDto(Guid JobId, DateTimeOffset EnqueuedAt);

    private sealed record ImportJobDto(
        Guid JobId,
        string SourceConnector,
        string Status,
        ImportCountsDto Counts,
        DateTimeOffset EnqueuedAt,
        DateTimeOffset? StartedAt,
        DateTimeOffset? CompletedAt,
        string? FailureReason,
        IReadOnlyList<string> Errors);

    private sealed record ImportCountsDto(
        int Extracted,
        int Transformed,
        int ValidationFailed,
        int Loaded,
        int New,
        int Updated,
        int Notified);

    private sealed record ContentDto(
        Guid Id,
        string ExternalId,
        string SourceSystem,
        string Type,
        string Title,
        string Slug,
        DateTimeOffset ImportedAt,
        IReadOnlyDictionary<string, string> Metadata);
}
