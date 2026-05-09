namespace CmsImporter.Infrastructure.Messaging;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string HostName { get; set; } = "localhost";

    public int Port { get; set; } = 5672;

    public string UserName { get; set; } = "cms";

    public string Password { get; set; } = "cms";

    public string VirtualHost { get; set; } = "/";

    public string Exchange { get; set; } = "cms.content";

    public string RoutingKeyPrefix { get; set; } = "cms.content.imported";
}
