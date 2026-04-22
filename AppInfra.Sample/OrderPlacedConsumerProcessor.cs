using AppInfra.Messaging.Kafka;
using AppInfra.Messaging.Abstractions;

namespace AppInfra.Sample;

internal sealed class OrderPlacedConsumerProcessor : IEventProcessor<OrderPlacedEvent>
{
    private readonly ILogger<OrderPlacedConsumerProcessor> _logger;

    public OrderPlacedConsumerProcessor(ILogger<OrderPlacedConsumerProcessor> logger)
    {
        _logger = logger;
    }

    public Task ProcessEventAsync(
        OrderPlacedEvent @event,
        EventContext context,
        CancellationToken cancellationToken)
    {
        context.Attributes.TryGetValue(KafkaEventContextAttributes.Partition, out var partition);
        _logger.LogInformation(
            "Orders consumer: order {OrderId} at {PlacedAt}; key={MessageKey}, partition={Partition}, headers={HeaderCount}",
            @event.OrderId,
            @event.PlacedAt,
            context.Key,
            partition,
            context.Headers.Count);
        return Task.CompletedTask;
    }
}
