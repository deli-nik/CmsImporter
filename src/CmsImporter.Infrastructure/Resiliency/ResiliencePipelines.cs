using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

using RabbitMQ.Client.Exceptions;

namespace CmsImporter.Infrastructure.Resiliency;

/// <summary>
/// Polly v8 resilience pipelines used by the Infrastructure layer. Registered as named
/// pipelines so consumers can resolve them by key from
/// <see cref="Polly.Registry.ResiliencePipelineProvider{TKey}"/>.
/// </summary>
public static class ResiliencePipelines
{
    /// <summary>
    /// Registers the RabbitMQ-publish (<see cref="ResiliencePipelineKeys.RabbitMqPublish"/>) and
    /// database-upsert (<see cref="ResiliencePipelineKeys.DatabaseUpsert"/>) pipelines on the
    /// service collection.
    /// </summary>
    /// <remarks>
    /// <para>RabbitMQ publish: 5 retries with jittered exponential backoff (base 500ms), trip on
    /// 50% failure ratio over a 30s sampling window, 15s break.</para>
    /// <para>Database upsert: 3 retries with jittered exponential backoff (base 200ms) on
    /// transient <see cref="DbUpdateException"/>.</para>
    /// </remarks>
    public static IServiceCollection AddCmsImporterResiliencePipelines(
        this IServiceCollection services)
    {
        services.AddResiliencePipeline(ResiliencePipelineKeys.RabbitMqPublish, builder =>
        {
            builder
                .AddRetry(new RetryStrategyOptions
                {
                    ShouldHandle = new PredicateBuilder()
                        .Handle<BrokerUnreachableException>()
                        .Handle<AlreadyClosedException>()
                        .Handle<OperationInterruptedException>(),
                    MaxRetryAttempts = 5,
                    Delay = TimeSpan.FromMilliseconds(500),
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                })
                .AddCircuitBreaker(new CircuitBreakerStrategyOptions
                {
                    ShouldHandle = new PredicateBuilder()
                        .Handle<BrokerUnreachableException>()
                        .Handle<AlreadyClosedException>(),
                    FailureRatio = 0.5,
                    MinimumThroughput = 4,
                    SamplingDuration = TimeSpan.FromSeconds(30),
                    BreakDuration = TimeSpan.FromSeconds(15),
                });
        });

        services.AddResiliencePipeline(ResiliencePipelineKeys.DatabaseUpsert, builder =>
        {
            builder.AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder()
                    .Handle<DbUpdateException>(),
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(200),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
            });
        });

        return services;
    }
}
