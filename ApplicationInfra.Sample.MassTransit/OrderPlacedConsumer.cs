using ApplicationInfra.Messaging.Abstractions;
using Microsoft.Extensions.Logging;

namespace ApplicationInfra.Sample.MassTransit;

internal sealed class OrderPlacedConsumer : IEventProcessor<OrderPlacedMessage>
{
    private readonly ILogger<OrderPlacedConsumer> _logger;

    public OrderPlacedConsumer(ILogger<OrderPlacedConsumer> logger)
    {
        _logger = logger;
    }

    public Task ProcessEventAsync(OrderPlacedMessage @event, EventContext context, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received OrderPlaced — OrderId={OrderId}, PlacedAt={PlacedAt}",
            @event.OrderId,
            @event.PlacedAt);
        return Task.CompletedTask;
    }
}
