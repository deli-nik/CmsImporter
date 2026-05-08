using System.Collections.Concurrent;

using CmsImporter.Application.Abstractions;
using CmsImporter.Application.Pipeline;

namespace CmsImporter.Application.Services;

public sealed class InMemoryImportProgressTracker : IImportProgressTracker
{
    private readonly ConcurrentDictionary<Guid, ImportProgress> _jobs = new();

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

    public ImportProgress? Get(Guid jobId) =>
        _jobs.TryGetValue(jobId, out var progress) ? progress : null;

    public IReadOnlyList<ImportProgress> Snapshot() => _jobs.Values.ToArray();
}
