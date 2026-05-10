namespace CmsImporter.Infrastructure.Resiliency;

/// <summary>
/// String keys for the named Polly v8 resilience pipelines registered via
/// <c>AddResiliencePipeline</c>. Consumers resolve the pipeline by name from the
/// <see cref="Polly.Registry.ResiliencePipelineProvider{TKey}"/>.
/// </summary>
public static class ResiliencePipelineKeys
{
    /// <summary>Retry + circuit-breaker for RabbitMQ publishes.</summary>
    public const string RabbitMqPublish = "rabbitmq.publish";

    /// <summary>Retry on transient EF Core save errors.</summary>
    public const string DatabaseUpsert = "database.upsert";

    /// <summary>Reserved for HTTP source connectors that opt out of <c>AddStandardResilienceHandler</c>.</summary>
    public const string HttpSource = "http.source";
}
