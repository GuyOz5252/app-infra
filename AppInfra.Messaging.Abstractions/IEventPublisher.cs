namespace AppInfra.Messaging.Abstractions;

public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default);

    Task PublishAsync<TEvent>(TEvent @event, PublishMetadata? metadata, CancellationToken cancellationToken = default);
}
