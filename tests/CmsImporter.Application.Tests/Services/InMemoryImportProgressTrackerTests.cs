using CmsImporter.Application.Pipeline;
using CmsImporter.Application.Services;
using CmsImporter.Core.Abstractions;

namespace CmsImporter.Application.Tests.Services;

[TestFixture]
public sealed class InMemoryImportProgressTrackerTests
{
    private InMemoryImportProgressTracker _tracker = null!;

    [SetUp]
    public void SetUp() => _tracker = new InMemoryImportProgressTracker();

    [Test]
    public void Register_NewJob_ReturnsProgressWithMatchingId()
    {
        var job = NewJob();

        var progress = _tracker.Register(job);

        Assert.Multiple(() =>
        {
            Assert.That(progress.JobId, Is.EqualTo(job.Id));
            Assert.That(progress.SourceConnector, Is.EqualTo(job.SourceConnector));
            Assert.That(progress.Status, Is.EqualTo(ImportStatus.Queued));
        });
    }

    [Test]
    public void Register_DuplicateJobId_Throws()
    {
        var job = NewJob();
        _tracker.Register(job);

        Assert.That(() => _tracker.Register(job), Throws.InvalidOperationException);
    }

    [Test]
    public void Get_UnknownJob_ReturnsNull()
    {
        Assert.That(_tracker.Get(Guid.NewGuid()), Is.Null);
    }

    [Test]
    public void Get_KnownJob_ReturnsRegisteredInstance()
    {
        var job = NewJob();
        var registered = _tracker.Register(job);

        var fetched = _tracker.Get(job.Id);

        Assert.That(fetched, Is.SameAs(registered));
    }

    [Test]
    public void Snapshot_ReturnsAllRegisteredJobs()
    {
        var job1 = NewJob();
        var job2 = NewJob();
        _tracker.Register(job1);
        _tracker.Register(job2);

        var snapshot = _tracker.Snapshot();

        Assert.That(snapshot.Select(p => p.JobId), Is.EquivalentTo(new[] { job1.Id, job2.Id }));
    }

    [Test]
    public void Register_IsThreadSafeAcrossManyJobs()
    {
        const int total = 1_000;

        Parallel.For(0, total, _ => _tracker.Register(NewJob()));

        Assert.That(_tracker.Snapshot(), Has.Count.EqualTo(total));
    }

    private static ImportJob NewJob() => new()
    {
        Id = Guid.NewGuid(),
        SourceConnector = "test",
        Options = SourceConnectorOptions.Empty,
        EnqueuedAt = DateTimeOffset.UtcNow,
    };
}
