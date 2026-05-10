namespace CmsImporter.Infrastructure.Messaging;

/// <summary>
/// Configuration for the RabbitMQ event publisher. Bound from the <see cref="SectionName"/>
/// section of <c>appsettings.json</c> at startup.
/// </summary>
public sealed class RabbitMqOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "RabbitMq";

    /// <summary>RabbitMQ broker hostname.</summary>
    public string HostName { get; set; } = "localhost";

    /// <summary>AMQP port (default 5672).</summary>
    public int Port { get; set; } = 5672;

    /// <summary>Username for the AMQP connection.</summary>
    public string UserName { get; set; } = "cms";

    /// <summary>Password for the AMQP connection.</summary>
    public string Password { get; set; } = "cms";

    /// <summary>Virtual host (defaults to <c>/</c>).</summary>
    public string VirtualHost { get; set; } = "/";

    /// <summary>Topic exchange name. Declared durable on connection.</summary>
    public string Exchange { get; set; } = "cms.content";

    /// <summary>
    /// Routing-key prefix. The publisher appends <c>.{sourceSystem}.{type}</c> for
    /// <c>ContentImportedEvent</c> messages so subscribers can pattern-match by source/type.
    /// </summary>
    public string RoutingKeyPrefix { get; set; } = "cms.content.imported";
}
