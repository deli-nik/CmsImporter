namespace CmsImporter.Core.Abstractions;

public interface IEventPublisher
{
    Task PublishAsync<TEvent>(
        TEvent @event,
        CancellationToken cancellationToken = default) where TEvent : class;

    Task PublishManyAsync<TEvent>(
        IReadOnlyCollection<TEvent> events,
        CancellationToken cancellationToken = default) where TEvent : class;
}
