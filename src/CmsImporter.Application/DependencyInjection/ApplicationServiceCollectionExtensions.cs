using CmsImporter.Application.Abstractions;
using CmsImporter.Application.Pipeline;
using CmsImporter.Application.Queries;
using CmsImporter.Application.Services;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CmsImporter.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
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

        services.AddSingleton<ExtractStage>();
        services.AddSingleton<TransformStage>();
        services.AddSingleton<ValidateStage>();
        services.AddSingleton<LoadStage>();
        services.AddSingleton<NotifyStage>();
        services.AddSingleton<ImportOrchestrator>();

        services.AddSingleton<IImportProgressTracker, InMemoryImportProgressTracker>();
        services.AddSingleton<ContentQueryService>();

        return services;
    }
}
