namespace AppInfra.Kafka.Abstract;

public interface IKafkaEventProcessor<in TEvent>
{
    Task ProcessEventAsync(TEvent @event, CancellationToken cancellationToken);
}
