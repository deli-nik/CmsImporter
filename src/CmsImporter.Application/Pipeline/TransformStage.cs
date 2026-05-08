using System.Text;

using CmsImporter.Core.DTOs;
using CmsImporter.Core.Entities;
using CmsImporter.Core.ValueObjects;

namespace CmsImporter.Application.Pipeline;

public sealed class TransformStage(TimeProvider timeProvider)
{
    public ContentItem Execute(RawContent raw)
    {
        ArgumentNullException.ThrowIfNull(raw);

        var type = ParseType(raw.Type);
        var slug = string.IsNullOrWhiteSpace(raw.Slug) ? Slugify(raw.Title) : raw.Slug.Trim();
        var importedAt = timeProvider.GetUtcNow();

        return new ContentItem
        {
            ExternalId = raw.ExternalId,
            SourceSystem = raw.SourceSystem,
            Type = type,
            Title = raw.Title,
            Slug = slug,
            Author = raw.Author,
            PublishedAt = raw.PublishedAt,
            Body = MapBody(raw),
            Metadata = raw.Metadata is null
                ? new Dictionary<string, string>()
                : new Dictionary<string, string>(raw.Metadata),
            ImportedAt = importedAt,
        };
    }

    private static ContentBody MapBody(RawContent raw) =>
        new()
        {
            Format = raw.BodyFormat,
            Raw = raw.BodyRaw,
            Blocks = raw.BodyBlocks?
                .Select(b => new ContentBlock
                {
                    Type = b.Type,
                    Content = b.Content,
                    Attributes = b.Attributes,
                })
                .ToList(),
        };

    private static ContentType ParseType(string raw) =>
        raw.Trim().ToLowerInvariant() switch
        {
            "page" => ContentType.Page,
            "article" => ContentType.Article,
            "post" => ContentType.Article,
            "media" or "asset" => ContentType.Media,
            _ => ContentType.Unknown,
        };

    private static string Slugify(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return "untitled";
        }

        var builder = new StringBuilder(input.Length);
        var lastWasDash = false;

        foreach (var ch in input.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(ch);
                lastWasDash = false;
            }
            else if (!lastWasDash && builder.Length > 0)
            {
                builder.Append('-');
                lastWasDash = true;
            }
        }

        if (builder.Length > 0 && builder[^1] == '-')
        {
            builder.Length--;
        }

        return builder.Length == 0 ? "untitled" : builder.ToString();
    }
}
