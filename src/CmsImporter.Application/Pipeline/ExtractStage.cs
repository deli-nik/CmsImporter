using System.Diagnostics;
using System.Runtime.CompilerServices;

using CmsImporter.Application.Abstractions;
using CmsImporter.Application.Telemetry;
using CmsImporter.Core.DTOs;

using Microsoft.Extensions.Logging;

namespace CmsImporter.Application.Pipeline;

public sealed class ExtractStage(
    ISourceConnectorRegistry registry,
    ILogger<ExtractStage> logger)
{
    public async IAsyncEnumerable<RawContent> ExecuteAsync(
        ImportJob job,
        ImportProgress progress,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var connector = registry.Resolve(job.SourceConnector);

        using var activity = ImportActivitySource.Instance.StartActivity("Import.Extract");
        
        activity?.SetTag("import.job_id", job.Id);
        activity?.SetTag("import.source", job.SourceConnector);

        logger.LogInformation(
            "Extracting from connector {SourceConnector} for job {JobId}",
            job.SourceConnector, job.Id);

        await foreach (var raw in connector.ReadAsync(job.Options, cancellationToken))
        {
            progress.IncrementExtracted();
            yield return raw;
        }

        activity?.SetTag("import.items_extracted", progress.ItemsExtracted);
    }
}
