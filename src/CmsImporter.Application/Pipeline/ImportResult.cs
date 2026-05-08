namespace CmsImporter.Application.Pipeline;

public sealed record ImportResult
{
    public required Guid JobId { get; init; }

    public required ImportStatus Status { get; init; }

    public required int ItemsExtracted { get; init; }

    public required int ItemsTransformed { get; init; }

    public required int ItemsValidationFailed { get; init; }

    public required int ItemsLoaded { get; init; }

    public required int NewItems { get; init; }

    public required int UpdatedItems { get; init; }

    public required int ItemsNotified { get; init; }

    public required TimeSpan Duration { get; init; }

    public string? FailureReason { get; init; }

    public IReadOnlyList<string> Errors { get; init; } = [];
}
