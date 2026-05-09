using System.Threading.Channels;

using CmsImporter.Application.Abstractions;
using CmsImporter.Application.Pipeline;
using CmsImporter.Core.Abstractions;
using CmsImporter.WebApi.Models;

using Microsoft.AspNetCore.Http.HttpResults;

namespace CmsImporter.WebApi.Endpoints;

public static class ImportEndpoints
{
    public static IEndpointRouteBuilder MapImportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/imports").WithTags("Imports");

        group.MapPost("/", EnqueueImport)
            .WithSummary("Enqueue a new import job")
            .Produces<EnqueueResponse>(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/{id:guid}", GetImport)
            .WithSummary("Get the status and counts for an import job")
            .Produces<ImportJobResponse>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/", ListImports)
            .WithSummary("List all import jobs (in-memory snapshot)")
            .Produces<IReadOnlyList<ImportJobResponse>>();

        group.MapGet("/connectors", ListConnectors)
            .WithSummary("List available source connectors")
            .Produces<IReadOnlyList<string>>();

        return app;
    }

    private static async Task<Results<Accepted<EnqueueResponse>, BadRequest<string>>> EnqueueImport(
        CreateImportRequest request,
        Channel<ImportJob> channel,
        IImportProgressTracker tracker,
        ISourceConnectorRegistry registry,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Source))
        {
            return TypedResults.BadRequest("Source is required.");
        }

        try
        {
            _ = registry.Resolve(request.Source);
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.BadRequest(ex.Message);
        }

        var job = new ImportJob
        {
            Id = Guid.NewGuid(),
            SourceConnector = request.Source,
            Options = new SourceConnectorOptions
            {
                Settings = request.Config ?? new Dictionary<string, string>(),
            },
            EnqueuedAt = timeProvider.GetUtcNow(),
        };

        tracker.Register(job);
        await channel.Writer.WriteAsync(job, cancellationToken);

        return TypedResults.Accepted(
            $"/imports/{job.Id}",
            new EnqueueResponse { JobId = job.Id, EnqueuedAt = job.EnqueuedAt });
    }

    private static Results<Ok<ImportJobResponse>, NotFound> GetImport(
        Guid id,
        IImportProgressTracker tracker)
    {
        var progress = tracker.Get(id);
        return progress is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(ImportJobResponse.From(progress));
    }

    private static Ok<IReadOnlyList<ImportJobResponse>> ListImports(IImportProgressTracker tracker)
    {
        IReadOnlyList<ImportJobResponse> response = tracker.Snapshot()
            .OrderByDescending(p => p.EnqueuedAt)
            .Select(ImportJobResponse.From)
            .ToArray();

        return TypedResults.Ok(response);
    }

    private static Ok<IReadOnlyList<string>> ListConnectors(ISourceConnectorRegistry registry) =>
        TypedResults.Ok(registry.AvailableConnectors);
}

public sealed record EnqueueResponse
{
    public required Guid JobId { get; init; }

    public required DateTimeOffset EnqueuedAt { get; init; }
}
