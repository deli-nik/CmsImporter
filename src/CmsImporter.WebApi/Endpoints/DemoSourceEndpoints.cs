namespace CmsImporter.WebApi.Endpoints;

/// <summary>
/// Dev-only mock REST source so the HttpRest connector is demoable without spinning up a separate
/// service. Returns a paginated JSON payload in the shape HttpRestSourceConnector expects.
/// </summary>
public static class DemoSourceEndpoints
{
    public static IEndpointRouteBuilder MapDemoSourceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/demo/source-feed").WithTags("Demo Source (dev only)");

        group.MapGet("/", (int page = 0, int pageSize = 2) =>
        {
            var allItems = SeedItems;
            var skip = page * pageSize;
            var pageItems = allItems.Skip(skip).Take(pageSize).ToArray();
            var hasMore = skip + pageItems.Length < allItems.Length;

            return Results.Ok(new PageResponse
            {
                Items = pageItems,
                HasMore = hasMore,
            });
        }).WithSummary("Mock paginated source feed for the HttpRest connector demo");

        return app;
    }

    private static readonly DemoItem[] SeedItems =
    [
        new()
        {
            ExternalId = "rest-page-100",
            Type = "Page",
            Title = "About us (from REST)",
            Slug = "about-us",
            Author = "Comms",
            PublishedAt = new DateTimeOffset(2024, 4, 1, 12, 0, 0, TimeSpan.Zero),
            BodyFormat = "markdown",
            BodyRaw = "# About\n\nWe started in 2018...",
            Metadata = new Dictionary<string, string>
            {
                ["category"] = "company",
                ["tags"] = "about,history",
            },
        },
        new()
        {
            ExternalId = "rest-article-200",
            Type = "Article",
            Title = "Engineering blog: building event-driven imports",
            Slug = "engineering-event-driven-imports",
            Author = "Engineering",
            PublishedAt = new DateTimeOffset(2024, 6, 12, 15, 30, 0, TimeSpan.Zero),
            BodyFormat = "markdown",
            BodyRaw = "Our import pipeline uses Channel<T>, Polly, and RabbitMQ...",
            Metadata = new Dictionary<string, string>
            {
                ["category"] = "engineering",
                ["tags"] = "architecture,events",
            },
        },
        new()
        {
            ExternalId = "rest-page-101",
            Type = "Page",
            Title = "Careers (from REST)",
            Slug = "careers",
            Author = "People Ops",
            PublishedAt = new DateTimeOffset(2024, 5, 20, 9, 0, 0, TimeSpan.Zero),
            BodyFormat = "markdown",
            BodyRaw = "# Careers\n\nWe're hiring across engineering, sales, and design.",
            Metadata = new Dictionary<string, string>
            {
                ["category"] = "company",
                ["tags"] = "hiring,careers",
            },
        },
    ];

    private sealed record PageResponse
    {
        public required IReadOnlyList<DemoItem> Items { get; init; }

        public required bool HasMore { get; init; }
    }

    private sealed record DemoItem
    {
        public required string ExternalId { get; init; }

        public required string Type { get; init; }

        public required string Title { get; init; }

        public required string Slug { get; init; }

        public string? Author { get; init; }

        public DateTimeOffset? PublishedAt { get; init; }

        public required string BodyFormat { get; init; }

        public required string BodyRaw { get; init; }

        public IReadOnlyDictionary<string, string>? Metadata { get; init; }
    }
}
