using ApplicationInfra.Sample.MassTransit.Protobuf;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace ApplicationInfra.Sample.MassTransit;

internal sealed class OrderShippedConsumer : IConsumer<OrderShipped>
{
    private readonly ILogger<OrderShippedConsumer> _logger;

    public OrderShippedConsumer(ILogger<OrderShippedConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<OrderShipped> context)
    {
        var shippedAt = DateTimeOffset.FromUnixTimeMilliseconds(context.Message.ShippedAtUnixMillis);

        _logger.LogInformation(
            "Received OrderShipped — OrderId={OrderId}, Tracking={TrackingNumber}, ShippedAt={ShippedAt}",
            context.Message.OrderId,
            context.Message.TrackingNumber,
            shippedAt);

        return Task.CompletedTask;
    }
}
