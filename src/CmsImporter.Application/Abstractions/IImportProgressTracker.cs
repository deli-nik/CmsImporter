using CmsImporter.Application.Pipeline;

namespace CmsImporter.Application.Abstractions;

/// <summary>
/// Process-wide registry of <see cref="ImportProgress"/> objects keyed by job id. Backs the
/// <c>GET /imports/{id}</c> read endpoint; survives across DI scopes (the orchestrator is scoped
/// per job, but the tracker is singleton).
/// </summary>
public interface IImportProgressTracker
{
    /// <summary>Creates a progress entry for a freshly enqueued job.</summary>
    /// <exception cref="InvalidOperationException">Thrown when the job id is already known.</exception>
    ImportProgress Register(ImportJob job);

    /// <summary>Returns the progress for a job, or <see langword="null"/> if it doesn't exist.</summary>
    ImportProgress? Get(Guid jobId);

    /// <summary>Returns a snapshot of every tracked job.</summary>
    IReadOnlyList<ImportProgress> Snapshot();
}
