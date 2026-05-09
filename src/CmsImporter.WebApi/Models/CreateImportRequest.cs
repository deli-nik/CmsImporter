namespace CmsImporter.WebApi.Models;

public sealed record CreateImportRequest
{
    public required string Source { get; init; }

    public IReadOnlyDictionary<string, string>? Config { get; init; }
}
