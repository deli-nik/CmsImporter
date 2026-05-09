using CmsImporter.Application.Pipeline;

namespace CmsImporter.WebApi.Models;

public sealed record ImportJobResponse
{
    public required Guid JobId { get; init; }

    public required string SourceConnector { get; init; }

    public required string Status { get; init; }

    public required ImportCounts Counts { get; init; }

    public required DateTimeOffset EnqueuedAt { get; init; }

    public DateTimeOffset? StartedAt { get; init; }

    public DateTimeOffset? CompletedAt { get; init; }

    public string? FailureReason { get; init; }

    public IReadOnlyList<string> Errors { get; init; } = [];

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

public sealed record ImportCounts
{
    public required int Extracted { get; init; }

    public required int Transformed { get; init; }

    public required int ValidationFailed { get; init; }

    public required int Loaded { get; init; }

    public required int New { get; init; }

    public required int Updated { get; init; }

    public required int Notified { get; init; }
}
