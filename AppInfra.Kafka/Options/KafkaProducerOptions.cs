namespace AppInfra.Kafka.Options;

public sealed class KafkaProducerOptions
{
    public string BootstrapServers { get; set; }
    public string Topic { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
}
