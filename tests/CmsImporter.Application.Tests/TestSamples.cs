using CmsImporter.Core.DTOs;

namespace CmsImporter.Application.Tests;

internal static class TestSamples
{
    public static RawContent NewRaw(
        string externalId = "ext-1",
        string sourceSystem = "test-source",
        string type = "Page",
        string title = "Hello",
        string? slug = null,
        string? author = null,
        DateTimeOffset? publishedAt = null,
        string bodyFormat = "text/plain",
        string bodyRaw = "body",
        IReadOnlyList<RawContentBlock>? bodyBlocks = null,
        IReadOnlyDictionary<string, string>? metadata = null) =>
        new()
        {
            ExternalId = externalId,
            SourceSystem = sourceSystem,
            Type = type,
            Title = title,
            Slug = slug,
            Author = author,
            PublishedAt = publishedAt,
            BodyFormat = bodyFormat,
            BodyRaw = bodyRaw,
            BodyBlocks = bodyBlocks,
            Metadata = metadata,
        };
}
