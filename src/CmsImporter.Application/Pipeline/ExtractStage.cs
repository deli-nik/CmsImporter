using System.Diagnostics;
using System.Runtime.CompilerServices;

using CmsImporter.Application.Abstractions;
using CmsImporter.Application.Telemetry;
using CmsImporter.Core.DTOs;

using Microsoft.Extensions.Logging;

namespace CmsImporter.Application.Pipeline;

/// <summary>
/// First stage of the import pipeline: resolves the source connector for a job and exposes its
/// stream as an <see cref="IAsyncEnumerable{T}"/> of <see cref="RawContent"/>. Wraps the call in
/// an <c>Import.Extract</c> tracing span and increments <see cref="ImportProgress.ItemsExtracted"/>
/// for every item that flows through.
/// </summary>
public sealed class ExtractStage(
    ISourceConnectorRegistry registry,
    ILogger<ExtractStage> logger)
{
    /// <summary>
    /// Streams the source's content. Lazy: each <c>yield return</c> is pulled on demand by the
    /// downstream consumer (typically <see cref="ImportOrchestrator"/>'s <c>Parallel.ForEachAsync</c>).
    /// </summary>
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
