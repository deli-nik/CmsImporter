using CmsImporter.WebApi;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace CmsImporter.IntegrationTests;

public sealed class CmsImporterTestFactory(
    string postgresConnectionString,
    string rabbitHost,
    int rabbitPort,
    string rabbitUser,
    string rabbitPassword) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = postgresConnectionString,
                ["RabbitMq:HostName"] = rabbitHost,
                ["RabbitMq:Port"] = rabbitPort.ToString(),
                ["RabbitMq:UserName"] = rabbitUser,
                ["RabbitMq:Password"] = rabbitPassword,
                ["RabbitMq:Exchange"] = "cms.content",
                ["RabbitMq:RoutingKeyPrefix"] = "cms.content.imported",
            });
        });
    }
}
