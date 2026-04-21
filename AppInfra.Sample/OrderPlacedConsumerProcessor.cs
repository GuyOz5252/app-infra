using AppInfra.Kafka.Abstract;

namespace AppInfra.Sample;

internal sealed class OrderPlacedConsumerProcessor : IKafkaEventProcessor<OrderPlacedEvent>
{
    private readonly ILogger<OrderPlacedConsumerProcessor> _logger;

    public OrderPlacedConsumerProcessor(ILogger<OrderPlacedConsumerProcessor> logger)
    {
        _logger = logger;
    }

    public Task ProcessEventAsync(OrderPlacedEvent @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Orders consumer: received order {OrderId} at {PlacedAt}", @event.OrderId, @event.PlacedAt);
        return Task.CompletedTask;
    }
}
