using System.Text.Json;

using CmsImporter.Core.Abstractions;
using CmsImporter.Core.Events;
using CmsImporter.Infrastructure.Resiliency;
using CmsImporter.Infrastructure.Serialization;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Polly;
using Polly.Registry;

using RabbitMQ.Client;

namespace CmsImporter.Infrastructure.Messaging;

public sealed class RabbitMqEventPublisher : IEventPublisher, IAsyncDisposable
{
    private readonly RabbitMqOptions _options;

    private readonly ResiliencePipeline _resilience;

    private readonly ILogger<RabbitMqEventPublisher> _logger;

    private readonly SemaphoreSlim _initLock = new(1, 1);

    private readonly SemaphoreSlim _publishLock = new(1, 1);

    private IConnection? _connection;

    private IChannel? _channel;

    private bool _disposed;

    public RabbitMqEventPublisher(
        IOptions<RabbitMqOptions> options,
        ResiliencePipelineProvider<string> pipelineProvider,
        ILogger<RabbitMqEventPublisher> logger)
    {
        _options = options.Value;
        _resilience = pipelineProvider.GetPipeline(ResiliencePipelineKeys.RabbitMqPublish);
        _logger = logger;
    }

    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : class =>
        PublishManyAsync([@event], cancellationToken);

    public async Task PublishManyAsync<TEvent>(
        IReadOnlyCollection<TEvent> events,
        CancellationToken cancellationToken = default)
        where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(events);
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (events.Count == 0)
        {
            return;
        }

        await EnsureConnectedAsync(cancellationToken);

        await _publishLock.WaitAsync(cancellationToken);
        try
        {
            await _resilience.ExecuteAsync(
                async ct =>
                {
                    foreach (var evt in events)
                    {
                        var routingKey = BuildRoutingKey(evt);
                        var body = JsonSerializer.SerializeToUtf8Bytes(evt, JsonDefaults.Web);
                        var props = new BasicProperties
                        {
                            ContentType = "application/json",
                            Persistent = true,
                            MessageId = Guid.NewGuid().ToString("N"),
                            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
                            Type = typeof(TEvent).Name,
                        };

                        await _channel!.BasicPublishAsync(
                            exchange: _options.Exchange,
                            routingKey: routingKey,
                            mandatory: false,
                            basicProperties: props,
                            body: body,
                            cancellationToken: ct);
                    }
                },
                cancellationToken);

            _logger.LogDebug(
                "Published {Count} {EventType} message(s) to exchange {Exchange}",
                events.Count, typeof(TEvent).Name, _options.Exchange);
        }
        finally
        {
            _publishLock.Release();
        }
    }

    private async Task EnsureConnectedAsync(CancellationToken cancellationToken)
    {
        if (_channel is { IsOpen: true })
        {
            return;
        }

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_channel is { IsOpen: true })
            {
                return;
            }

            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password,
                VirtualHost = _options.VirtualHost,
                AutomaticRecoveryEnabled = true,
            };

            _connection = await factory.CreateConnectionAsync(cancellationToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

            await _channel.ExchangeDeclareAsync(
                exchange: _options.Exchange,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Connected to RabbitMQ at {HostName}:{Port}, declared exchange {Exchange}",
                _options.HostName, _options.Port, _options.Exchange);
        }
        finally
        {
            _initLock.Release();
        }
    }

    private string BuildRoutingKey<TEvent>(TEvent @event) where TEvent : class =>
        @event switch
        {
            ContentImportedEvent c =>
                $"{_options.RoutingKeyPrefix}.{c.SourceSystem.ToLowerInvariant()}.{c.Type.ToString().ToLowerInvariant()}",
            _ => $"{_options.RoutingKeyPrefix}.{typeof(TEvent).Name.ToLowerInvariant()}",
        };

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_channel is not null)
        {
            await _channel.DisposeAsync();
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        _initLock.Dispose();
        _publishLock.Dispose();
    }
}
