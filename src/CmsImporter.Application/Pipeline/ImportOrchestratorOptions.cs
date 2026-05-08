namespace CmsImporter.Application.Pipeline;

public sealed class ImportOrchestratorOptions
{
    public const string SectionName = "Import";

    public int TransformParallelism { get; set; } = Math.Max(1, Environment.ProcessorCount - 1);

    public int ChannelCapacity { get; set; } = 1000;

    public int LoadBatchSize { get; set; } = 200;
}
