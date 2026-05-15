namespace ApplicationInfra.Messaging.Kafka.Options;

public sealed class KafkaConsumerOptions
{
    public required string BootstrapServers { get; set; }
    public required string Topic { get; set; }
    public required string ConsumerGroup { get; set; }
    public required string Password { get; set; }
}
