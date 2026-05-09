using CmsImporter.Application.Abstractions;
using CmsImporter.Core.Abstractions;
using CmsImporter.Infrastructure.Connectors;
using CmsImporter.Infrastructure.Messaging;
using CmsImporter.Infrastructure.Persistence;
using CmsImporter.Infrastructure.Resiliency;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

namespace CmsImporter.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddCmsImporterInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // EF Core + PostgreSQL
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException(
                "Connection string 'Postgres' is not configured.");

        services.AddDbContext<AppDbContext>(opts =>
            opts.UseNpgsql(connectionString, npg => npg.EnableRetryOnFailure(maxRetryCount: 3)));

        services.AddScoped<IContentRepository, EfContentRepository>();

        // RabbitMQ
        services.AddOptions<RabbitMqOptions>()
            .Bind(configuration.GetSection(RabbitMqOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();

        // Polly resilience pipelines (RabbitMQ + DB).
        services.AddCmsImporterResiliencePipelines();

        // Source connectors — one ISourceConnector per implementation; registry resolves by Name.
        services.AddSingleton<ISourceConnector, FileSystemJsonSourceConnector>();

        services.AddHttpClient(HttpRestSourceConnector.HttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        }).AddStandardResilienceHandler();

        services.AddSingleton<ISourceConnector, HttpRestSourceConnector>();

        services.AddSingleton<ISourceConnectorRegistry, SourceConnectorRegistry>();

        return services;
    }
}
