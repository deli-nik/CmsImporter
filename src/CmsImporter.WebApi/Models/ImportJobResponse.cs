using CmsImporter.Application.Pipeline;

namespace CmsImporter.WebApi.Models;

/// <summary>
/// API response DTO representing an import job's current state. Constructed from an
/// <see cref="ImportProgress"/> via <see cref="From"/>.
/// </summary>
public sealed record ImportJobResponse
{
    /// <summary>Unique identifier of the import job.</summary>
    public required Guid JobId { get; init; }

    /// <summary>Name of the source connector used by the job.</summary>
    public required string SourceConnector { get; init; }

    /// <summary>Current lifecycle status as a string (e.g., <c>"Queued"</c>, <c>"Running"</c>, <c>"Completed"</c>).</summary>
    public required string Status { get; init; }

    /// <summary>Item throughput counters across all pipeline stages.</summary>
    public required ImportCounts Counts { get; init; }

    /// <summary>UTC instant the job was accepted by the API.</summary>
    public required DateTimeOffset EnqueuedAt { get; init; }

    /// <summary>UTC instant the orchestrator began processing the job, or <see langword="null"/> if not yet started.</summary>
    public DateTimeOffset? StartedAt { get; init; }

    /// <summary>UTC instant the job reached a terminal state, or <see langword="null"/> if still in progress.</summary>
    public DateTimeOffset? CompletedAt { get; init; }

    /// <summary>Top-line failure message when <see cref="Status"/> is <c>"Failed"</c>.</summary>
    public string? FailureReason { get; init; }

    /// <summary>Per-item error messages collected during the run.</summary>
    public IReadOnlyList<string> Errors { get; init; } = [];

    /// <summary>Projects an <see cref="ImportProgress"/> snapshot into an <see cref="ImportJobResponse"/>.</summary>
    public static ImportJobResponse From(ImportProgress progress) =>
        new()
        {
            JobId = progress.JobId,
            SourceConnector = progress.SourceConnector,
            Status = progress.Status.ToString(),
            EnqueuedAt = progress.EnqueuedAt,
            StartedAt = progress.StartedAt,
            CompletedAt = progress.CompletedAt,
            FailureReason = progress.FailureReason,
            Errors = progress.Errors.ToArray(),
            Counts = new ImportCounts
            {
                Extracted = progress.ItemsExtracted,
                Transformed = progress.ItemsTransformed,
                ValidationFailed = progress.ItemsValidationFailed,
                Loaded = progress.ItemsLoaded,
                New = progress.NewItems,
                Updated = progress.UpdatedItems,
                Notified = progress.ItemsNotified,
            },
        };
}

/// <summary>Pipeline throughput counters returned as part of <see cref="ImportJobResponse"/>.</summary>
public sealed record ImportCounts
{
    /// <summary>Items pulled out of the source connector.</summary>
    public required int Extracted { get; init; }

    /// <summary>Items that passed the transform and validate stages.</summary>
    public required int Transformed { get; init; }

    /// <summary>Items rejected by validation or that threw during transform.</summary>
    public required int ValidationFailed { get; init; }

    /// <summary>Items written to the database (sum of <see cref="New"/> + <see cref="Updated"/>).</summary>
    public required int Loaded { get; init; }

    /// <summary>Items inserted for the first time on this run.</summary>
    public required int New { get; init; }

    /// <summary>Items that already existed and were updated in place.</summary>
    public required int Updated { get; init; }

    /// <summary>Number of <c>ContentImportedEvent</c> messages successfully published to RabbitMQ.</summary>
    public required int Notified { get; init; }
}
