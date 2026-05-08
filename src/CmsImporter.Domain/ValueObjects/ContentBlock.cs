namespace CmsImporter.Domain.ValueObjects;

public sealed record ContentBlock(
    string Type,
    string Content,
    IReadOnlyDictionary<string, string>? Attributes);
