namespace CmsImporter.Application.Pipeline;

/// <summary>
/// Immutable snapshot of an import job at a terminal state. The orchestrator returns this from
/// <c>RunAsync</c>; the API maps it onto the response DTO.
/// </summary>
public sealed record ImportResult
{
    /// <summary>Job identifier.</summary>
    public required Guid JobId { get; init; }

    /// <summary>Terminal status — Completed, Failed, or Cancelled.</summary>
    public required ImportStatus Status { get; init; }

    /// <summary>Items pulled from the source.</summary>
    public required int ItemsExtracted { get; init; }

    /// <summary>Items that passed transform + validate.</summary>
    public required int ItemsTransformed { get; init; }

    /// <summary>Items rejected by validation or transform errors.</summary>
    public required int ItemsValidationFailed { get; init; }

    /// <summary>Items written to the database (new + updated).</summary>
    public required int ItemsLoaded { get; init; }

    /// <summary>Of <see cref="ItemsLoaded"/>, the count that were inserts.</summary>
    public required int NewItems { get; init; }

    /// <summary>Of <see cref="ItemsLoaded"/>, the count that were updates.</summary>
    public required int UpdatedItems { get; init; }

    /// <summary>Number of upstream-notification events successfully published.</summary>
    public required int ItemsNotified { get; init; }

    /// <summary>Wall-clock time spent in <c>RunAsync</c>.</summary>
    public required TimeSpan Duration { get; init; }

    /// <summary>Top-line reason set when <see cref="Status"/> is <see cref="ImportStatus.Failed"/>.</summary>
    public string? FailureReason { get; init; }

    /// <summary>Per-item error messages collected during the run.</summary>
    public IReadOnlyList<string> Errors { get; init; } = [];
}
