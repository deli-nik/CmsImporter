namespace CmsImporter.Core.ValueObjects;

public sealed record ContentBody
{
    public required string Format { get; init; }

    public required string Raw { get; init; }

    public IReadOnlyList<ContentBlock>? Blocks { get; init; }

    public static ContentBody Empty(string format = "text/plain") =>
        new() { Format = format, Raw = string.Empty, Blocks = null };
}
