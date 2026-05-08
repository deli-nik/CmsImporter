using System.Collections.Concurrent;

namespace CmsImporter.Application.Pipeline;

public sealed class ImportProgress
{
    private int _itemsExtracted;

    private int _itemsTransformed;

    private int _itemsValidationFailed;

    private int _itemsLoaded;

    private int _itemsNotified;

    private int _newItems;

    private int _updatedItems;

    public required Guid JobId { get; init; }

    public required string SourceConnector { get; init; }

    public required DateTimeOffset EnqueuedAt { get; init; }

    public ImportStatus Status { get; set; } = ImportStatus.Queued;

    public DateTimeOffset? StartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public string? FailureReason { get; set; }

    public ConcurrentBag<string> Errors { get; } = new();

    public int ItemsExtracted => Volatile.Read(ref _itemsExtracted);

    public int ItemsTransformed => Volatile.Read(ref _itemsTransformed);

    public int ItemsValidationFailed => Volatile.Read(ref _itemsValidationFailed);

    public int ItemsLoaded => Volatile.Read(ref _itemsLoaded);

    public int ItemsNotified => Volatile.Read(ref _itemsNotified);

    public int NewItems => Volatile.Read(ref _newItems);

    public int UpdatedItems => Volatile.Read(ref _updatedItems);

    public int IncrementExtracted() => Interlocked.Increment(ref _itemsExtracted);

    public int IncrementTransformed() => Interlocked.Increment(ref _itemsTransformed);

    public int IncrementValidationFailed() => Interlocked.Increment(ref _itemsValidationFailed);

    public void AddLoaded(int newCount, int updatedCount)
    {
        Interlocked.Add(ref _itemsLoaded, newCount + updatedCount);
        Interlocked.Add(ref _newItems, newCount);
        Interlocked.Add(ref _updatedItems, updatedCount);
    }

    public int AddNotified(int count) => Interlocked.Add(ref _itemsNotified, count);

    public void RecordError(string message) => Errors.Add(message);
}
