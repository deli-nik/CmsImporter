using System.Collections.Concurrent;

namespace CmsImporter.Application.Pipeline;

/// <summary>
/// Mutable, thread-safe snapshot of one import job's state. Updated concurrently by N parallel
/// transform workers and a single load/notify consumer; counter access uses
/// <see cref="Interlocked"/> + <see cref="Volatile"/> rather than locks.
/// </summary>
public sealed class ImportProgress
{
    private int _itemsExtracted;

    private int _itemsTransformed;

    private int _itemsValidationFailed;

    private int _itemsLoaded;

    private int _itemsNotified;

    private int _newItems;

    private int _updatedItems;

    /// <summary>Job identifier — same value as <c>ImportJob.Id</c>.</summary>
    public required Guid JobId { get; init; }

    /// <summary>Connector name used by this job.</summary>
    public required string SourceConnector { get; init; }

    /// <summary>UTC instant the job was accepted.</summary>
    public required DateTimeOffset EnqueuedAt { get; init; }

    /// <summary>Lifecycle state. Mutated only by the orchestrator's main thread.</summary>
    public ImportStatus Status { get; set; } = ImportStatus.Queued;

    /// <summary>UTC instant the orchestrator started processing this job.</summary>
    public DateTimeOffset? StartedAt { get; set; }

    /// <summary>UTC instant the job reached a terminal state.</summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>Top-line failure reason set when <see cref="Status"/> is <see cref="ImportStatus.Failed"/>.</summary>
    public string? FailureReason { get; set; }

    /// <summary>Per-item error messages collected during the run. <see cref="ConcurrentBag{T}"/>
    /// because parallel workers may append simultaneously.</summary>
    public ConcurrentBag<string> Errors { get; } = new();

    /// <summary>Items pulled out of the source connector so far.</summary>
    public int ItemsExtracted => Volatile.Read(ref _itemsExtracted);

    /// <summary>Items that passed transform + validate.</summary>
    public int ItemsTransformed => Volatile.Read(ref _itemsTransformed);

    /// <summary>Items rejected because they failed validation or threw during transform.</summary>
    public int ItemsValidationFailed => Volatile.Read(ref _itemsValidationFailed);

    /// <summary>Items that reached the database (sum of new + updated).</summary>
    public int ItemsLoaded => Volatile.Read(ref _itemsLoaded);

    /// <summary>Number of <c>ContentImportedEvent</c> messages successfully published.</summary>
    public int ItemsNotified => Volatile.Read(ref _itemsNotified);

    /// <summary>Items that were inserted on this run.</summary>
    public int NewItems => Volatile.Read(ref _newItems);

    /// <summary>Items that were updated in place on this run.</summary>
    public int UpdatedItems => Volatile.Read(ref _updatedItems);

    /// <summary>Atomically increments <see cref="ItemsExtracted"/>; returns the new value.</summary>
    public int IncrementExtracted() => Interlocked.Increment(ref _itemsExtracted);

    /// <summary>Atomically increments <see cref="ItemsTransformed"/>.</summary>
    public int IncrementTransformed() => Interlocked.Increment(ref _itemsTransformed);

    /// <summary>Atomically increments <see cref="ItemsValidationFailed"/>.</summary>
    public int IncrementValidationFailed() => Interlocked.Increment(ref _itemsValidationFailed);

    /// <summary>Atomically adds the loaded breakdown across the new/updated/total counters.</summary>
    public void AddLoaded(int newCount, int updatedCount)
    {
        Interlocked.Add(ref _itemsLoaded, newCount + updatedCount);
        Interlocked.Add(ref _newItems, newCount);
        Interlocked.Add(ref _updatedItems, updatedCount);
    }

    /// <summary>Atomically adds <paramref name="count"/> to <see cref="ItemsNotified"/>.</summary>
    public int AddNotified(int count) => Interlocked.Add(ref _itemsNotified, count);

    /// <summary>Appends an error message; safe to call from any thread.</summary>
    public void RecordError(string message) => Errors.Add(message);
}
