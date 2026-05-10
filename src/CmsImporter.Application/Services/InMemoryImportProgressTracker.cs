using System.Collections.Concurrent;

using CmsImporter.Application.Abstractions;
using CmsImporter.Application.Pipeline;

namespace CmsImporter.Application.Services;

/// <summary>
/// Default <see cref="IImportProgressTracker"/> implementation backed by a
/// <see cref="ConcurrentDictionary{TKey, TValue}"/>. Intentionally non-persistent: progress is
/// lost on restart, which is fine for the demo. A production deployment might replace this with
/// a Redis-backed implementation so the same tracker view is shared across replicas.
/// </summary>
public sealed class InMemoryImportProgressTracker : IImportProgressTracker
{
    private readonly ConcurrentDictionary<Guid, ImportProgress> _jobs = new();

    /// <inheritdoc />
    public ImportProgress Register(ImportJob job)
    {
        var progress = new ImportProgress
        {
            JobId = job.Id,
            SourceConnector = job.SourceConnector,
            EnqueuedAt = job.EnqueuedAt,
        };

        if (!_jobs.TryAdd(job.Id, progress))
        {
            throw new InvalidOperationException($"Job {job.Id} is already registered.");
        }

        return progress;
    }

    /// <inheritdoc />
    public ImportProgress? Get(Guid jobId) =>
        _jobs.TryGetValue(jobId, out var progress) ? progress : null;

    /// <inheritdoc />
    public IReadOnlyList<ImportProgress> Snapshot() => _jobs.Values.ToArray();
}
