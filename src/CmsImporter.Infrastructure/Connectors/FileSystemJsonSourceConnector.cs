using System.Runtime.CompilerServices;
using System.Text.Json;

using CmsImporter.Core.Abstractions;
using CmsImporter.Core.DTOs;
using CmsImporter.Infrastructure.Serialization;

using Microsoft.Extensions.Logging;

namespace CmsImporter.Infrastructure.Connectors;

/// <summary>
/// Source connector that reads JSON files from a directory. Uses
/// <see cref="JsonSerializer.DeserializeAsyncEnumerable{TValue}(System.IO.Stream, JsonSerializerOptions, CancellationToken)"/>
/// so a multi-gigabyte export is processed one item at a time without ever buffering the full
/// file into memory.
/// </summary>
/// <remarks>
/// <para>Connector options:</para>
/// <list type="bullet">
///   <item><c>path</c> (required) — directory containing the JSON files.</item>
///   <item><c>sourceSystem</c> (required) — logical name injected onto every item.</item>
///   <item><c>pattern</c> (optional, defaults to <c>*.json</c>) — file name glob.</item>
/// </list>
/// </remarks>
public sealed class FileSystemJsonSourceConnector(
    ILogger<FileSystemJsonSourceConnector> logger) : ISourceConnector
{
    /// <inheritdoc />
    public string Name => "FileSystem";

    /// <inheritdoc />
    public async IAsyncEnumerable<RawContent> ReadAsync(
        SourceConnectorOptions options,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        var path = options.Require("path");
        var pattern = options.GetOrDefault("pattern") ?? "*.json";
        var sourceSystem = options.Require("sourceSystem");

        if (!Directory.Exists(path))
        {
            throw new DirectoryNotFoundException($"Source directory not found: {path}");
        }

        logger.LogInformation(
            "FileSystemJsonSourceConnector reading {Pattern} from {Path}",
            pattern, path);

        foreach (var file in Directory.EnumerateFiles(path, pattern, SearchOption.TopDirectoryOnly))
        {
            cancellationToken.ThrowIfCancellationRequested();

            await using var stream = File.OpenRead(file);

            // Streaming deserialization — never buffers the whole file into memory.
            // Works for any file size; one item at a time enters the heap.
            await foreach (var item in JsonSerializer.DeserializeAsyncEnumerable<FileItem>(
                stream, JsonDefaults.Web, cancellationToken))
            {
                if (item is null)
                {
                    continue;
                }

                yield return MapToRawContent(item, sourceSystem);
            }
        }
    }

    private static RawContent MapToRawContent(FileItem item, string sourceSystem) =>
        new()
        {
            ExternalId = item.ExternalId,
            SourceSystem = sourceSystem,
            Type = item.Type,
            Title = item.Title,
            Slug = item.Slug,
            Author = item.Author,
            PublishedAt = item.PublishedAt,
            BodyFormat = item.BodyFormat,
            BodyRaw = item.BodyRaw,
            BodyBlocks = item.BodyBlocks?.Select(b => new RawContentBlock
            {
                Type = b.Type,
                Content = b.Content,
                Attributes = b.Attributes,
            }).ToList(),
            Metadata = item.Metadata,
        };

    /// <summary>JSON-shaped DTO for one item inside a source file.</summary>
    private sealed record FileItem
    {
        public string ExternalId { get; init; } = string.Empty;

        public string Type { get; init; } = string.Empty;

        public string Title { get; init; } = string.Empty;

        public string? Slug { get; init; }

        public string? Author { get; init; }

        public DateTimeOffset? PublishedAt { get; init; }

        public string BodyFormat { get; init; } = "text/plain";

        public string BodyRaw { get; init; } = string.Empty;

        public IReadOnlyList<FileBlock>? BodyBlocks { get; init; }

        public IReadOnlyDictionary<string, string>? Metadata { get; init; }
    }

    /// <summary>JSON-shaped DTO for one body block inside a source file.</summary>
    private sealed record FileBlock
    {
        public string Type { get; init; } = string.Empty;

        public string Content { get; init; } = string.Empty;

        public IReadOnlyDictionary<string, string>? Attributes { get; init; }
    }
}
