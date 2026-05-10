using System.Text;
using System.Text.Json;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CmsImporter.SampleSubscriber;

public static class Program
{
    private const string Exchange = "cms.content";

    private const string RoutingKeyPattern = "cms.content.imported.#";

    private const string DefaultHost = "localhost";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static async Task<int> Main(string[] args)
    {
        var hostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? DefaultHost;
        var userName = Environment.GetEnvironmentVariable("RABBITMQ_USER") ?? "cms";
        var password = Environment.GetEnvironmentVariable("RABBITMQ_PASS") ?? "cms";

        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine("CmsImporter SampleSubscriber");
        Console.WriteLine($"  RabbitMQ:    {hostName}");
        Console.WriteLine($"  Exchange:    {Exchange}");
        Console.WriteLine($"  Routing key: {RoutingKeyPattern}");
        Console.WriteLine();

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            Console.WriteLine();
            Console.WriteLine("Ctrl+C received, shutting down...");
            cts.Cancel();
        };

        var factory = new ConnectionFactory
        {
            HostName = hostName,
            UserName = userName,
            Password = password,
            AutomaticRecoveryEnabled = true,
        };

        await using var connection = await factory.CreateConnectionAsync(cts.Token);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cts.Token);

        await channel.ExchangeDeclareAsync(
            exchange: Exchange,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cts.Token);

        var queue = await channel.QueueDeclareAsync(
            queue: string.Empty,
            durable: false,
            exclusive: true,
            autoDelete: true,
            cancellationToken: cts.Token);

        await channel.QueueBindAsync(
            queue: queue.QueueName,
            exchange: Exchange,
            routingKey: RoutingKeyPattern,
            cancellationToken: cts.Token);

        Console.WriteLine($"Listening on queue '{queue.QueueName}'. Press Ctrl+C to exit.");
        Console.WriteLine();

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            var body = Encoding.UTF8.GetString(ea.Body.ToArray());
            var routingKey = ea.RoutingKey;
            var messageId = ea.BasicProperties.MessageId ?? "(none)";
            var eventType = ea.BasicProperties.Type ?? "(unknown)";

            var pretty = TryPrettyPrint(body);
            var color = routingKey.Contains(".article", StringComparison.OrdinalIgnoreCase)
                ? ConsoleColor.Cyan
                : ConsoleColor.Yellow;

            var prev = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine($"--- {DateTime.Now:HH:mm:ss}  {eventType}  msg={messageId}");
            Console.WriteLine($"    routing-key: {routingKey}");
            Console.ForegroundColor = prev;
            Console.WriteLine(pretty);
            Console.WriteLine();

            await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: cts.Token);
        };

        await channel.BasicConsumeAsync(
            queue: queue.QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: cts.Token);

        try
        {
            await Task.Delay(Timeout.Infinite, cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected on Ctrl+C.
        }

        Console.WriteLine("Disconnected.");
        return 0;
    }

    private static string TryPrettyPrint(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(doc.RootElement, JsonOptions);
        }
        catch
        {
            return json;
        }
    }
}
