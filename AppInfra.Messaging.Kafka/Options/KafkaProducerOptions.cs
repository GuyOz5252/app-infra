namespace AppInfra.Messaging.Kafka.Options;

public sealed class KafkaProducerOptions
{
    public string BootstrapServers { get; set; }
    public string Topic { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string SecurityProtocol { get; set; }
    public string SaslMechanism { get; set; }
}
