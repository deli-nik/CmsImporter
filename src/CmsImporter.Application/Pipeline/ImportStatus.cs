namespace CmsImporter.Application.Pipeline;

/// <summary>
/// Lifecycle states of an import job. The transitions are
/// <c>Queued → Running → (Completed | Failed | Cancelled)</c> — terminal states are mutually exclusive.
/// </summary>
public enum ImportStatus
{
    /// <summary>Accepted by the API and waiting on the worker channel.</summary>
    Queued = 0,

    /// <summary>Currently executing in the orchestrator.</summary>
    Running = 1,

    /// <summary>Finished without an unhandled exception.</summary>
    Completed = 2,

    /// <summary>Aborted by an unhandled exception; details on <c>ImportProgress.FailureReason</c>.</summary>
    Failed = 3,

    /// <summary>Aborted via the caller's <see cref="CancellationToken"/>.</summary>
    Cancelled = 4,
}
