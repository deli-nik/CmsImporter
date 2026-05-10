using CmsImporter.Application.Abstractions;
using CmsImporter.Application.Pipeline;
using CmsImporter.Application.Queries;
using CmsImporter.Application.Services;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CmsImporter.Application.DependencyInjection;

/// <summary>
/// DI wiring for the Application layer. Registers pipeline stages, the orchestrator, the
/// in-memory progress tracker, and the read-side query service with appropriate lifetimes.
/// </summary>
public static class ApplicationServiceCollectionExtensions
{
    /// <summary>
    /// Adds Application-layer services to the container. Bind options from
    /// <see cref="ImportOrchestratorOptions.SectionName"/> (default <c>Import</c>) of
    /// <paramref name="configuration"/>.
    /// </summary>
    public static IServiceCollection AddCmsImporterApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<ImportOrchestratorOptions>()
            .Bind(configuration.GetSection(ImportOrchestratorOptions.SectionName))
            .ValidateOnStart();

        services.TryAddSingleton(TimeProvider.System);

        // Stateless / pure stages — singleton.
        services.AddSingleton<ExtractStage>();
        services.AddSingleton<TransformStage>();
        services.AddSingleton<ValidateStage>();
        services.AddSingleton<NotifyStage>();

        // LoadStage transitively depends on IContentRepository (scoped DbContext).
        services.AddScoped<LoadStage>();

        // Orchestrator depends on LoadStage → must be scoped. The BackgroundService
        // creates one scope per import job and resolves the orchestrator from it.
        services.AddScoped<ImportOrchestrator>();

        // Tracker survives across scopes so /imports/{id} can read in-flight state.
        services.AddSingleton<IImportProgressTracker, InMemoryImportProgressTracker>();

        // Read service depends on the scoped repository.
        services.AddScoped<ContentQueryService>();

        return services;
    }
}
