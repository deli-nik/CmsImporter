using CmsImporter.Application.Pipeline;

namespace CmsImporter.Application.Tests.Pipeline;

[TestFixture]
public sealed class ImportProgressTests
{
    [Test]
    public void DefaultStatus_IsQueued()
    {
        var progress = NewProgress();

        Assert.That(progress.Status, Is.EqualTo(ImportStatus.Queued));
        Assert.That(progress.ItemsExtracted, Is.Zero);
    }

    [Test]
    public void IncrementCounters_AreThreadSafeUnderHighConcurrency()
    {
        const int total = 5_000;
        var progress = NewProgress();

        Parallel.For(0, total, _ =>
        {
            progress.IncrementExtracted();
            progress.IncrementTransformed();
        });

        Assert.Multiple(() =>
        {
            Assert.That(progress.ItemsExtracted, Is.EqualTo(total));
            Assert.That(progress.ItemsTransformed, Is.EqualTo(total));
        });
    }

    [Test]
    public void AddLoaded_AccumulatesNewAndUpdatedCorrectly()
    {
        var progress = NewProgress();

        progress.AddLoaded(newCount: 7, updatedCount: 3);
        progress.AddLoaded(newCount: 2, updatedCount: 5);

        Assert.Multiple(() =>
        {
            Assert.That(progress.NewItems, Is.EqualTo(9));
            Assert.That(progress.UpdatedItems, Is.EqualTo(8));
            Assert.That(progress.ItemsLoaded, Is.EqualTo(17));
        });
    }

    [Test]
    public void Errors_IsThreadSafe_AndPreservesAllMessages()
    {
        const int total = 1_000;
        var progress = NewProgress();

        Parallel.For(0, total, i => progress.RecordError($"err-{i}"));

        Assert.That(progress.Errors, Has.Count.EqualTo(total));
    }

    private static ImportProgress NewProgress() => new()
    {
        JobId = Guid.NewGuid(),
        SourceConnector = "test",
        EnqueuedAt = DateTimeOffset.UtcNow,
    };
}
