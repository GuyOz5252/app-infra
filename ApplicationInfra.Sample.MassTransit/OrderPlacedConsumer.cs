using MassTransit;

namespace ApplicationInfra.Sample.MassTransit;

public class OrderPlacedConsumer : IConsumer<OrderPlacedMessage>
{
    private readonly ILogger<OrderPlacedConsumer> _logger;

    public OrderPlacedConsumer(ILogger<OrderPlacedConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<OrderPlacedMessage> context)
    {
        _logger.LogInformation(
            "Received OrderPlaced — OrderId={OrderId}, PlacedAt={PlacedAt}",
            context.Message.OrderId,
            context.Message.PlacedAt);
        return Task.CompletedTask;
    }
}
