namespace CmsImporter.Domain.ValueObjects;

public sealed record ContentBody(
    string Format,
    string Raw,
    IReadOnlyList<ContentBlock>? Blocks)
{
    public static ContentBody Empty(string format = "text/plain") =>
        new(format, string.Empty, null);
}
