using CmsImporter.Application.Queries;
using CmsImporter.Core.Entities;
using CmsImporter.WebApi.Models;

using Microsoft.AspNetCore.Http.HttpResults;

namespace CmsImporter.WebApi.Endpoints;

/// <summary>
/// Minimal-API endpoints for querying imported content. All routes are grouped under <c>/content</c>.
/// </summary>
public static class ContentEndpoints
{
    /// <summary>Registers the <c>GET /content</c> search endpoint on <paramref name="app"/>.</summary>
    public static IEndpointRouteBuilder MapContentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/content").WithTags("Content");

        group.MapGet("/", SearchContent)
            .WithSummary("Search imported content with deferred IQueryable composition")
            .Produces<IReadOnlyList<ContentResponse>>();

        return app;
    }

    private static async Task<Ok<IReadOnlyList<ContentResponse>>> SearchContent(
        ContentQueryService service,
        CancellationToken cancellationToken,
        string? sourceSystem = null,
        ContentType? type = null,
        DateTimeOffset? since = null,
        string? titleContains = null,
        int limit = 50,
        int offset = 0)
    {
        var criteria = new ContentSearchCriteria
        {
            SourceSystem = sourceSystem,
            Type = type,
            SinceImportedAt = since,
            TitleContains = titleContains,
            Limit = limit,
            Offset = offset,
        };

        var items = await service.SearchAsync(criteria, cancellationToken);

        IReadOnlyList<ContentResponse> response = items.Select(ContentResponse.From).ToArray();
        return TypedResults.Ok(response);
    }
}
