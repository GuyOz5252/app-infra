namespace ApplicationInfra.Messaging.Kafka.MassTransit.Options;

public sealed class MassTransitKafkaConsumerOptions
{
    public string BootstrapServers { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string ConsumerGroup { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string? Password { get; set; }
}
