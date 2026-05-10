namespace CmsImporter.Application.Pipeline;

/// <summary>
/// Tunables for the import pipeline. Bound to the <see cref="SectionName"/> configuration section
/// at startup; can be overridden via <c>appsettings.json</c>, environment variables, or command-line args.
/// </summary>
public sealed class ImportOrchestratorOptions
{
    /// <summary>The configuration-section name (e.g., <c>Import</c> in <c>appsettings.json</c>).</summary>
    public const string SectionName = "Import";

    /// <summary>Maximum concurrent transform+validate workers (passed to
    /// <see cref="ParallelOptions.MaxDegreeOfParallelism"/>). Defaults to <c>ProcessorCount - 1</c>
    /// to leave a core for the consumer.</summary>
    public int TransformParallelism { get; set; } = Math.Max(1, Environment.ProcessorCount - 1);

    /// <summary>Bounded capacity of the producer→consumer <c>Channel&lt;ContentItem&gt;</c>. Larger
    /// values absorb more burst, smaller values force backpressure earlier.</summary>
    public int ChannelCapacity { get; set; } = 1000;

    /// <summary>How many items the consumer accumulates before flushing one DB upsert + one event publish.</summary>
    public int LoadBatchSize { get; set; } = 200;
}
