namespace AppInfra.Kafka;

public interface IKafkaEventProcessor<in TEvent>
{
    Task ProcessEventAsync(TEvent @event, CancellationToken cancellationToken);
}
