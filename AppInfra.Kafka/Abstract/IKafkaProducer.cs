namespace AppInfra.Kafka.Abstract;

public interface IKafkaProducer
{
    Task ProduceAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default);
}
