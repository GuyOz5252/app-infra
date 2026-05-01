using ApplicationInfra.Messaging.Abstractions;
using MassTransit;
using MassTransit.KafkaIntegration;

namespace ApplicationInfra.Messaging.Kafka.MassTransit;

internal sealed class MassTransitEventPublisher<TEvent> : IEventPublisher
    where TEvent : class
{
    private readonly ITopicProducer<TEvent> _producer;

    public MassTransitEventPublisher(ITopicProducer<TEvent> producer)
    {
        _producer = producer;
    }

    public Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default)
    {
        return PublishAsync(@event, metadata: null, cancellationToken);
    }

    public Task PublishAsync<T>(T @event, PublishMetadata? metadata, CancellationToken cancellationToken = default)
    {
        if (@event is not TEvent typed)
        {
            throw new InvalidOperationException(
                $"This publisher is bound to {typeof(TEvent).Name}. Cannot publish {typeof(T).Name}.");
        }

        if (metadata?.Headers is { Count: > 0 } headers)
        {
            var pipe = Pipe.Execute<KafkaSendContext<TEvent>>(ctx =>
            {
                foreach (var (key, value) in headers)
                {
                    ctx.Headers.Set(key, value);
                }
            });
            return _producer.Produce(typed, pipe, cancellationToken);
        }

        return _producer.Produce(typed, cancellationToken);
    }
}
