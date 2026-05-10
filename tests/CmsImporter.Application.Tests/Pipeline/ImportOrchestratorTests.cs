using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

using CmsImporter.Application.Abstractions;
using CmsImporter.Application.Pipeline;
using CmsImporter.Core.Abstractions;
using CmsImporter.Core.DTOs;
using CmsImporter.Core.Entities;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

namespace CmsImporter.Application.Tests.Pipeline;

[TestFixture]
public sealed class ImportOrchestratorTests
{
    private static readonly DateTimeOffset FixedNow =
        new(2026, 5, 10, 12, 0, 0, TimeSpan.Zero);

    [Test]
    public async Task Run_HappyPath_LoadsAndPublishesAllItems()
    {
        var raws = Enumerable.Range(1, 10)
            .Select(i => TestSamples.NewRaw(externalId: $"id-{i}", title: $"Title {i}"))
            .ToList();

        var (orchestrator, repository, publisher, progress) = BuildOrchestrator(raws);

        var result = await orchestrator.RunAsync(NewJob(), progress, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
            Assert.That(result.ItemsExtracted, Is.EqualTo(10));
            Assert.That(result.ItemsTransformed, Is.EqualTo(10));
            Assert.That(result.ItemsValidationFailed, Is.Zero);
            Assert.That(result.ItemsLoaded, Is.EqualTo(10));
            Assert.That(result.NewItems, Is.EqualTo(10));
            Assert.That(result.UpdatedItems, Is.Zero);
            Assert.That(result.ItemsNotified, Is.EqualTo(10));
            Assert.That(repository.UpsertCallCount, Is.GreaterThanOrEqualTo(1));
            Assert.That(publisher.Published, Has.Count.EqualTo(10));
        });
    }

    [Test]
    public async Task Run_DropsInvalidItems_AndContinues()
    {
        // Mix of valid and invalid: 3 are missing required fields.
        var raws = new List<RawContent>
        {
            TestSamples.NewRaw(externalId: "ok-1"),
            TestSamples.NewRaw(externalId: ""),                       // invalid: empty externalId
            TestSamples.NewRaw(externalId: "ok-2"),
            TestSamples.NewRaw(externalId: "bad-3", type: "WidgetX"), // invalid: unknown type
            TestSamples.NewRaw(externalId: "ok-4"),
            TestSamples.NewRaw(externalId: "bad-5", title: ""),       // invalid: empty title
        };

        var (orchestrator, repository, publisher, progress) = BuildOrchestrator(raws);

        var result = await orchestrator.RunAsync(NewJob(), progress, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
            Assert.That(result.ItemsExtracted, Is.EqualTo(6));
            Assert.That(result.ItemsValidationFailed, Is.EqualTo(3));
            Assert.That(result.ItemsLoaded, Is.EqualTo(3));
            Assert.That(publisher.Published, Has.Count.EqualTo(3));
            Assert.That(result.Errors, Is.Not.Empty);
        });
    }

    [Test]
    public async Task Run_DedupsByExternalId_WithinBatch()
    {
        // Same externalId appears 3 times — last-wins dedup means 1 final upsert.
        var raws = new List<RawContent>
        {
            TestSamples.NewRaw(externalId: "duplicate", title: "v1"),
            TestSamples.NewRaw(externalId: "duplicate", title: "v2"),
            TestSamples.NewRaw(externalId: "duplicate", title: "v3"),
            TestSamples.NewRaw(externalId: "unique", title: "u"),
        };

        var (orchestrator, repository, publisher, progress) = BuildOrchestrator(raws);

        var result = await orchestrator.RunAsync(NewJob(), progress, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.ItemsExtracted, Is.EqualTo(4));
            Assert.That(result.ItemsTransformed, Is.EqualTo(4));
            // 4 items pass validation but dedup collapses 3 dupes → 2 distinct loaded.
            Assert.That(result.ItemsLoaded, Is.EqualTo(2));
            Assert.That(publisher.Published, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public async Task Run_ConnectorThrows_StatusFailed_ErrorRecorded()
    {
        var (orchestrator, _, _, progress) = BuildOrchestrator(
            connector: new ThrowingConnector(new InvalidOperationException("source down")));

        var result = await orchestrator.RunAsync(NewJob(), progress, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(ImportStatus.Failed));
            Assert.That(result.FailureReason, Does.Contain("source down"));
            Assert.That(result.Errors, Has.Some.Contain("InvalidOperationException"));
        });
    }

    [Test]
    public async Task Run_CallerCancels_StatusCancelled()
    {
        var raws = Enumerable.Range(1, 50)
            .Select(i => TestSamples.NewRaw(externalId: $"id-{i}"))
            .ToList();

        var slow = new SlowConnector(raws, TimeSpan.FromMilliseconds(50));
        var (orchestrator, _, _, progress) = BuildOrchestrator(connector: slow);

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(75));

        var result = await orchestrator.RunAsync(NewJob(), progress, cts.Token);

        Assert.That(result.Status, Is.EqualTo(ImportStatus.Cancelled));
    }

    private static ImportJob NewJob() => new()
    {
        Id = Guid.NewGuid(),
        SourceConnector = "test",
        Options = SourceConnectorOptions.Empty,
        EnqueuedAt = FixedNow,
    };

    private static (ImportOrchestrator Orchestrator, FakeRepository Repository, FakeEventPublisher Publisher, ImportProgress Progress)
        BuildOrchestrator(IEnumerable<RawContent>? raws = null, ISourceConnector? connector = null)
    {
        connector ??= new FakeConnector(raws ?? []);

        var registry = Substitute.For<ISourceConnectorRegistry>();
        registry.Resolve(Arg.Any<string>()).Returns(connector);

        var time = Substitute.For<TimeProvider>();
        time.GetUtcNow().Returns(FixedNow);

        var repository = new FakeRepository();
        var publisher = new FakeEventPublisher();

        var extract = new ExtractStage(registry, NullLogger<ExtractStage>.Instance);
        var transform = new TransformStage(time);
        var validate = new ValidateStage();
        var load = new LoadStage(repository, NullLogger<LoadStage>.Instance);
        var notify = new NotifyStage(publisher, NullLogger<NotifyStage>.Instance);

        var options = Options.Create(new ImportOrchestratorOptions
        {
            TransformParallelism = 4,
            ChannelCapacity = 100,
            LoadBatchSize = 50,
        });

        var orchestrator = new ImportOrchestrator(
            extract, transform, validate, load, notify,
            options, time, NullLogger<ImportOrchestrator>.Instance);

        var progress = new ImportProgress
        {
            JobId = Guid.NewGuid(),
            SourceConnector = "test",
            EnqueuedAt = FixedNow,
        };

        return (orchestrator, repository, publisher, progress);
    }

    private sealed class FakeConnector(IEnumerable<RawContent> items) : ISourceConnector
    {
        public string Name => "test";

        public async IAsyncEnumerable<RawContent> ReadAsync(
            SourceConnectorOptions options,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var item in items)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return item;
                await Task.Yield();
            }
        }
    }

    private sealed class ThrowingConnector(Exception toThrow) : ISourceConnector
    {
        public string Name => "throwing";

        public async IAsyncEnumerable<RawContent> ReadAsync(
            SourceConnectorOptions options,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            throw toThrow;
#pragma warning disable CS0162
            yield break;
#pragma warning restore CS0162
        }
    }

    private sealed class SlowConnector(IEnumerable<RawContent> items, TimeSpan perItemDelay) : ISourceConnector
    {
        public string Name => "slow";

        public async IAsyncEnumerable<RawContent> ReadAsync(
            SourceConnectorOptions options,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var item in items)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(perItemDelay, cancellationToken);
                yield return item;
            }
        }
    }

    private sealed class FakeRepository : IContentRepository
    {
        private readonly ConcurrentBag<ContentItem> _stored = new();

        public int UpsertCallCount { get; private set; }

        public IQueryable<ContentItem> Query() => _stored.AsQueryable();

        public Task<ContentItem?> FindByExternalIdAsync(
            string sourceSystem, string externalId, CancellationToken ct = default) =>
            Task.FromResult(_stored.FirstOrDefault(c => c.SourceSystem == sourceSystem && c.ExternalId == externalId));

        public Task<IReadOnlyDictionary<string, ContentItem>> FindByExternalIdsAsync(
            string sourceSystem, IReadOnlyCollection<string> externalIds, CancellationToken ct = default)
        {
            IReadOnlyDictionary<string, ContentItem> result = _stored
                .Where(c => c.SourceSystem == sourceSystem && externalIds.Contains(c.ExternalId))
                .ToDictionary(c => c.ExternalId);
            return Task.FromResult(result);
        }

        public Task<UpsertResult> UpsertManyAsync(
            IReadOnlyCollection<ContentItem> items, CancellationToken ct = default)
        {
            UpsertCallCount++;
            var newItems = new List<ContentItem>();
            foreach (var item in items)
            {
                if (item.Id == Guid.Empty)
                {
                    item.Id = Guid.NewGuid();
                }

                _stored.Add(item);
                newItems.Add(item);
            }

            return Task.FromResult(new UpsertResult { NewItems = newItems, UpdatedItems = [] });
        }
    }

    private sealed class FakeEventPublisher : IEventPublisher
    {
        public ConcurrentBag<object> Published { get; } = new();

        public Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default)
            where TEvent : class
        {
            Published.Add(@event);
            return Task.CompletedTask;
        }

        public Task PublishManyAsync<TEvent>(IReadOnlyCollection<TEvent> events, CancellationToken ct = default)
            where TEvent : class
        {
            foreach (var e in events)
            {
                Published.Add(e);
            }

            return Task.CompletedTask;
        }
    }
}
