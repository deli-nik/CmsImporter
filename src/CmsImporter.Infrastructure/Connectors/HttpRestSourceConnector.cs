using System.Runtime.CompilerServices;
using System.Text.Json;

using CmsImporter.Core.Abstractions;
using CmsImporter.Core.DTOs;
using CmsImporter.Infrastructure.Serialization;

using Microsoft.Extensions.Logging;

namespace CmsImporter.Infrastructure.Connectors;

public sealed class HttpRestSourceConnector(
    IHttpClientFactory httpClientFactory,
    ILogger<HttpRestSourceConnector> logger) : ISourceConnector
{
    public const string HttpClientName = "HttpSource";

    public string Name => "HttpRest";

    public async IAsyncEnumerable<RawContent> ReadAsync(
        SourceConnectorOptions options,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        var baseUrl = options.Require("baseUrl");
        var sourceSystem = options.Require("sourceSystem");
        var pageSize = int.TryParse(options.GetOrDefault("pageSize"), out var ps) ? ps : 100;

        var client = httpClientFactory.CreateClient(HttpClientName);

        logger.LogInformation(
            "HttpRestSourceConnector fetching from {BaseUrl} (pageSize={PageSize})",
            baseUrl, pageSize);

        var page = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var requestUrl = $"{baseUrl.TrimEnd('/')}?page={page}&pageSize={pageSize}";

            using var response = await client.GetAsync(
                requestUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

            var pageResult = await JsonSerializer.DeserializeAsync<PageResponse>(
                stream, JsonDefaults.Web, cancellationToken);

            if (pageResult is null || pageResult.Items.Count == 0)
            {
                yield break;
            }

            foreach (var item in pageResult.Items)
            {
                yield return MapToRawContent(item, sourceSystem);
            }

            if (!pageResult.HasMore)
            {
                yield break;
            }

            page++;
        }
    }

    private static RawContent MapToRawContent(HttpItem item, string sourceSystem) =>
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

    private sealed record PageResponse
    {
        public IReadOnlyList<HttpItem> Items { get; init; } = [];

        public bool HasMore { get; init; }
    }

    private sealed record HttpItem
    {
        public string ExternalId { get; init; } = string.Empty;

        public string Type { get; init; } = string.Empty;

        public string Title { get; init; } = string.Empty;

        public string? Slug { get; init; }

        public string? Author { get; init; }

        public DateTimeOffset? PublishedAt { get; init; }

        public string BodyFormat { get; init; } = "text/plain";

        public string BodyRaw { get; init; } = string.Empty;

        public IReadOnlyList<HttpBlock>? BodyBlocks { get; init; }

        public IReadOnlyDictionary<string, string>? Metadata { get; init; }
    }

    private sealed record HttpBlock
    {
        public string Type { get; init; } = string.Empty;

        public string Content { get; init; } = string.Empty;

        public IReadOnlyDictionary<string, string>? Attributes { get; init; }
    }
}
