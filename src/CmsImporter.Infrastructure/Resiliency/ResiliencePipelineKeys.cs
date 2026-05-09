namespace CmsImporter.Infrastructure.Resiliency;

public static class ResiliencePipelineKeys
{
    public const string RabbitMqPublish = "rabbitmq.publish";

    public const string DatabaseUpsert = "database.upsert";

    public const string HttpSource = "http.source";
}
