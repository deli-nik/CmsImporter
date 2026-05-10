using System.Collections.Concurrent;
using System.Text;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CmsImporter.IntegrationTests;

public sealed class TestSubscriber : IAsyncDisposable
{
    private readonly IConnection _connection;

    private readonly IChannel _channel;

    public ConcurrentQueue<ReceivedMessage> Received { get; } = new();

    private TestSubscriber(IConnection connection, IChannel channel)
    {
        _connection = connection;
        _channel = channel;
    }

    public static async Task<TestSubscriber> CreateAsync(
        string host, int port, string user, string pass, string exchange, string routingKey)
    {
        var factory = new ConnectionFactory
        {
            HostName = host,
            Port = port,
            UserName = user,
            Password = pass,
        };

        var connection = await factory.CreateConnectionAsync();
        var channel = await connection.CreateChannelAsync();

        await channel.ExchangeDeclareAsync(exchange, ExchangeType.Topic, durable: true);

        var queue = await channel.QueueDeclareAsync(
            queue: string.Empty, durable: false, exclusive: true, autoDelete: true);

        await channel.QueueBindAsync(queue.QueueName, exchange, routingKey);

        var subscriber = new TestSubscriber(connection, channel);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            subscriber.Received.Enqueue(new ReceivedMessage(
                RoutingKey: ea.RoutingKey,
                Body: Encoding.UTF8.GetString(ea.Body.ToArray())));

            await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
        };

        await channel.BasicConsumeAsync(queue.QueueName, autoAck: false, consumer);

        return subscriber;
    }

    public async ValueTask DisposeAsync()
    {
        await _channel.DisposeAsync();
        await _connection.DisposeAsync();
    }

    public sealed record ReceivedMessage(string RoutingKey, string Body);
}
