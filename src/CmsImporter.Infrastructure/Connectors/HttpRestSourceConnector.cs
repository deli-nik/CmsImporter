using System.Runtime.CompilerServices;
using System.Text.Json;

using CmsImporter.Core.Abstractions;
using CmsImporter.Core.DTOs;
using CmsImporter.Infrastructure.Serialization;

using Microsoft.Extensions.Logging;

namespace CmsImporter.Infrastructure.Connectors;

/// <summary>
/// Source connector that pages through a REST endpoint expecting <c>{ items: [...], hasMore }</c>
/// JSON responses. Uses <see cref="HttpCompletionOption.ResponseHeadersRead"/> + streaming
/// deserialisation so large pages don't materialise fully in memory. The HTTP client is
/// configured with <c>AddStandardResilienceHandler()</c> for retry / circuit-breaker.
/// </summary>
/// <remarks>
/// <para>Connector options:</para>
/// <list type="bullet">
///   <item><c>baseUrl</c> (required) — the endpoint that returns paginated content.</item>
///   <item><c>sourceSystem</c> (required) — logical name injected onto every item.</item>
///   <item><c>pageSize</c> (optional, defaults to 100) — page size sent as a query parameter.</item>
/// </list>
/// </remarks>
public sealed class HttpRestSourceConnector(
    IHttpClientFactory httpClientFactory,
    ILogger<HttpRestSourceConnector> logger) : ISourceConnector
{
    /// <summary>
    /// Named-client key registered via <c>AddHttpClient(HttpClientName, ...)</c>. The named
    /// client picks up the <c>AddStandardResilienceHandler()</c> chain.
    /// </summary>
    public const string HttpClientName = "HttpSource";

    /// <inheritdoc />
    public string Name => "HttpRest";

    /// <inheritdoc />
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

    /// <summary>The expected response shape from one paginated request.</summary>
    private sealed record PageResponse
    {
        public IReadOnlyList<HttpItem> Items { get; init; } = [];

        public bool HasMore { get; init; }
    }

    /// <summary>JSON-shaped DTO for one item inside a page response.</summary>
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

    /// <summary>JSON-shaped DTO for one body block inside an item.</summary>
    private sealed record HttpBlock
    {
        public string Type { get; init; } = string.Empty;

        public string Content { get; init; } = string.Empty;

        public IReadOnlyDictionary<string, string>? Attributes { get; init; }
    }
}
