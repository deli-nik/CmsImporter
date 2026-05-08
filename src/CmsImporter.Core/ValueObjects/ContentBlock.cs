namespace CmsImporter.Core.ValueObjects;

public sealed record ContentBlock
{
    public required string Type { get; init; }

    public required string Content { get; init; }

    public IReadOnlyDictionary<string, string>? Attributes { get; init; }
}
