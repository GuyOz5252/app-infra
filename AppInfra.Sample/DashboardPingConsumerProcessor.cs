using AppInfra.Kafka;

namespace AppInfra.Sample;

internal sealed class DashboardPingConsumerProcessor : IKafkaEventProcessor<DashboardPingEvent>
{
    private readonly ILogger<DashboardPingConsumerProcessor> _logger;

    public DashboardPingConsumerProcessor(ILogger<DashboardPingConsumerProcessor> logger)
    {
        _logger = logger;
    }

    public Task ProcessEventAsync(DashboardPingEvent @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Dashboard consumer: ping from {Source}, tick {Tick}",
            @event.Source,
            @event.Tick);
        return Task.CompletedTask;
    }
}
