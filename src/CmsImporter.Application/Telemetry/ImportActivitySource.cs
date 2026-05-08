using System.Diagnostics;

namespace CmsImporter.Application.Telemetry;

public static class ImportActivitySource
{
    public const string Name = "CmsImporter";

    public static readonly ActivitySource Instance = new(Name, "1.0.0");
}
