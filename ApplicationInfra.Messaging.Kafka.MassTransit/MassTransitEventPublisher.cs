using ApplicationInfra.Messaging.Abstractions;
using MassTransit;

namespace ApplicationInfra.Messaging.Kafka.MassTransit;

public class MassTransitEventPublisher<TEvent> : IEventPublisher 
    where TEvent : class
{
    private readonly ITopicProducer<TEvent> _producer;

    public MassTransitEventPublisher(ITopicProducer<TEvent> producer)
    {
        _producer = producer;
    }

    public Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default)
    {
        return PublishAsync(@event, null, cancellationToken);
    }

    public Task PublishAsync<T>(T @event, PublishMetadata? metadata, CancellationToken cancellationToken = default)
    {
        if (@event is not TEvent typed)
        {
            throw new InvalidOperationException(
                $"This publisher is bound to {typeof(TEvent).Name}. Cannot publish {typeof(T).Name}.");
        }

        if (metadata?.Headers is not { Count: > 0 } headers)
        {
            return _producer.Produce(typed, cancellationToken);
        }

        var pipe = Pipe.Execute<KafkaSendContext<TEvent>>(kafkaSendContext =>
        {
            foreach (var (key, value) in headers)
            {
                kafkaSendContext.Headers.Set(key, value);
            }
        });
        
        return _producer.Produce(typed, pipe, cancellationToken);
    }
}
