using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

using RabbitMQ.Client.Exceptions;

namespace CmsImporter.Infrastructure.Resiliency;

public static class ResiliencePipelines
{
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
