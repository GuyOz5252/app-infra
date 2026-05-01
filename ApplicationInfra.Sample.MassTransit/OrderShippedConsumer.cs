using ApplicationInfra.Messaging.Abstractions;
using ApplicationInfra.Sample.MassTransit.Protobuf;
using Microsoft.Extensions.Logging;

namespace ApplicationInfra.Sample.MassTransit;

internal sealed class OrderShippedConsumer : IEventProcessor<OrderShipped>
{
    private readonly ILogger<OrderShippedConsumer> _logger;

    public OrderShippedConsumer(ILogger<OrderShippedConsumer> logger)
    {
        _logger = logger;
    }

    public Task ProcessEventAsync(OrderShipped @event, EventContext context, CancellationToken cancellationToken)
    {
        var shippedAt = DateTimeOffset.FromUnixTimeMilliseconds(@event.ShippedAtUnixMillis);

        _logger.LogInformation(
            "Received OrderShipped — OrderId={OrderId}, Tracking={TrackingNumber}, ShippedAt={ShippedAt}",
            @event.OrderId,
            @event.TrackingNumber,
            shippedAt);

        return Task.CompletedTask;
    }
}
