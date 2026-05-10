using System.Threading.Channels;

using CmsImporter.Application.DependencyInjection;
using CmsImporter.Application.Pipeline;
using CmsImporter.Application.Telemetry;
using CmsImporter.Infrastructure.DependencyInjection;
using CmsImporter.Infrastructure.Persistence;
using CmsImporter.WebApi.BackgroundServices;
using CmsImporter.WebApi.Endpoints;

using Scalar.AspNetCore;

using Microsoft.EntityFrameworkCore;

using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

using Serilog;
using Serilog.Events;

namespace CmsImporter.WebApi;

/// <summary>
/// Entry point and host configuration for the CmsImporter Web API.
/// </summary>
public class Program
{
    /// <summary>
    /// Builds and runs the ASP.NET Core host. Applies EF Core migrations on startup, wires
    /// up Serilog logging, OpenTelemetry tracing, Swagger UI, and all API endpoints.
    /// Returns <c>0</c> on clean shutdown or <c>1</c> when the host terminates unexpectedly.
    /// </summary>
    public static async Task<int> Main(string[] args)
    {
        // Bootstrap logger — captures any failures during host construction.
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File(
                path: "logs/cms-importer-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14)
            .CreateBootstrapLogger();

        try
        {
            Log.Information("Starting CmsImporter.WebApi");

            var builder = WebApplication.CreateBuilder(args);

            // Replace the default logger with Serilog, reading config + DI services.
            builder.Host.UseSerilog((context, services, config) => config
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File(
                    path: "logs/cms-importer-.log",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 14,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}"));

            // Application + Infrastructure layers.
            builder.Services.AddCmsImporterApplication(builder.Configuration);
            builder.Services.AddCmsImporterInfrastructure(builder.Configuration);

            // Job channel — singleton bounded channel between the API (writer) and the worker (reader).
            builder.Services.AddSingleton(_ => Channel.CreateBounded<ImportJob>(new BoundedChannelOptions(100)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false,
            }));

            builder.Services.AddHostedService<ImportWorker>();

            // OpenAPI — built-in .NET 9 support; document served at /openapi/v1.json.
            builder.Services.AddOpenApi();

            // OpenTelemetry tracing — covers ASP.NET Core, HttpClient, EF Core, plus our import pipeline source.
            builder.Services.AddOpenTelemetry()
                .ConfigureResource(r => r.AddService("CmsImporter.WebApi"))
                .WithTracing(t => t
                    .AddSource(ImportActivitySource.Name)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation()
                    .AddConsoleExporter());

            // Health checks.
            builder.Services.AddHealthChecks()
                .AddDbContextCheck<AppDbContext>("postgres");

            var app = builder.Build();

            // Apply EF migrations on startup.
            await using (var scope = app.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                Log.Information("Applying EF Core migrations...");
                await db.Database.MigrateAsync();
                Log.Information("Migrations applied.");
            }

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi(); // served at /openapi/v1.json
                app.MapScalarApiReference(); // UI at /scalar/v1
                app.MapDemoSourceEndpoints();
            }

            app.UseSerilogRequestLogging();

            app.MapHealthChecks("/health");
            app.MapImportEndpoints();
            app.MapContentEndpoints();

            await app.RunAsync();

            return 0;
        }
        catch (Exception ex) when (ex is not HostAbortedException)
        {
            Log.Fatal(ex, "CmsImporter.WebApi terminated unexpectedly");
            return 1;
        }
        finally
        {
            Log.Information("CmsImporter.WebApi shutting down");
            await Log.CloseAndFlushAsync();
        }
    }
}
