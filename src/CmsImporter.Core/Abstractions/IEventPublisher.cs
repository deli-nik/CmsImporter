namespace CmsImporter.Core.Abstractions;

/// <summary>
/// Outbound event-publishing boundary. The default implementation publishes to RabbitMQ via a
/// topic exchange; the abstraction lets the pipeline stay transport-agnostic (an in-memory or
/// HTTP-webhook variant could substitute without touching application code).
/// </summary>
public interface IEventPublisher
{
    /// <summary>Publishes a single event.</summary>
    Task PublishAsync<TEvent>(
        TEvent @event,
        CancellationToken cancellationToken = default) where TEvent : class;

    /// <summary>
    /// Publishes a batch of events. Implementations may serialize publishes internally to honor
    /// the underlying transport's thread-safety constraints (e.g., RabbitMQ's <c>IChannel</c>).
    /// </summary>
    Task PublishManyAsync<TEvent>(
        IReadOnlyCollection<TEvent> events,
        CancellationToken cancellationToken = default) where TEvent : class;
}
