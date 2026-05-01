namespace ApplicationInfra.Messaging.Kafka.MassTransit.Options;

public sealed class MassTransitKafkaProducerOptions
{
    public string BootstrapServers { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string? Password { get; set; }
}
