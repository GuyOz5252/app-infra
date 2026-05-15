using ApplicationInfra.Messaging.Abstractions;
using ApplicationInfra.Sample.Protobuf;

namespace ApplicationInfra.Sample;

internal sealed class SampleOrderPlacedEventProcessor : IEventProcessor<SampleOrderPlaced>
{
    private readonly ILogger<SampleOrderPlacedEventProcessor> _logger;

    public SampleOrderPlacedEventProcessor(ILogger<SampleOrderPlacedEventProcessor> logger)
    {
        _logger = logger;
    }

    public Task ProcessEventAsync(
        SampleOrderPlaced @event,
        EventContext context,
        CancellationToken cancellationToken)
    {
        context.Attributes.TryGetValue("Partition", out var partition);
        _logger.LogInformation(
            "Proto orders consumer: order {OrderId} at unix millis {PlacedAtUnixMillis}; key={MessageKey}, partition={Partition}, headers={HeaderCount}",
            @event.OrderId,
            @event.PlacedAtUnixMillis,
            context.Key,
            partition,
            context.Headers.Count);
        return Task.CompletedTask;
    }
}
