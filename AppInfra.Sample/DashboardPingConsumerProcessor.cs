using AppInfra.Messaging.Abstractions;

namespace AppInfra.Sample;

internal sealed class DashboardPingConsumerProcessor : IEventProcessor<DashboardPingEvent>
{
    private readonly ILogger<DashboardPingConsumerProcessor> _logger;

    public DashboardPingConsumerProcessor(ILogger<DashboardPingConsumerProcessor> logger)
    {
        _logger = logger;
    }

    public Task ProcessEventAsync(
        DashboardPingEvent @event,
        EventContext context,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Dashboard consumer: ping from {Source}, tick {Tick}",
            @event.Source,
            @event.Tick);
        return Task.CompletedTask;
    }
}
