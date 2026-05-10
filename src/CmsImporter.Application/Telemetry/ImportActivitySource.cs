using System.Diagnostics;

namespace CmsImporter.Application.Telemetry;

/// <summary>
/// Centralised <see cref="System.Diagnostics.ActivitySource"/> for the import pipeline. Each
/// stage opens a child span ("Import.Extract", "Import.TransformOne", "Import.Load", ...) so
/// OpenTelemetry can build a full trace across the lifecycle of a job.
/// </summary>
public static class ImportActivitySource
{
    /// <summary>Activity-source name. Must be added to the OTel TracerProvider via
    /// <c>AddSource(ImportActivitySource.Name)</c> for spans to be exported.</summary>
    public const string Name = "CmsImporter";

    /// <summary>Process-wide singleton. Use <c>Instance.StartActivity("Import.Foo")</c>
    /// in a <c>using</c> to bracket a stage with a span.</summary>
    public static readonly ActivitySource Instance = new(Name, "1.0.0");
}
