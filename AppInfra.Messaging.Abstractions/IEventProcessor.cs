namespace AppInfra.Messaging.Abstractions;

public interface IEventProcessor<in TEvent>
{
    Task ProcessEventAsync(TEvent @event, EventContext context, CancellationToken cancellationToken);
}
