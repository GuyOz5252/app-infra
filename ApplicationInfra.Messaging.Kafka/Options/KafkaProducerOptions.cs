namespace ApplicationInfra.Messaging.Kafka.Options;

public sealed class KafkaProducerOptions
{
    public required string BootstrapServers { get; set; }
    public required string Topic { get; set; }
    public required string Username { get; set; }
    public required string Password { get; set; }
}
