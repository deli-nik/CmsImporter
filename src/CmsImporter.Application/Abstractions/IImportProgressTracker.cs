using CmsImporter.Application.Pipeline;

namespace CmsImporter.Application.Abstractions;

public interface IImportProgressTracker
{
    ImportProgress Register(ImportJob job);

    ImportProgress? Get(Guid jobId);

    IReadOnlyList<ImportProgress> Snapshot();
}
