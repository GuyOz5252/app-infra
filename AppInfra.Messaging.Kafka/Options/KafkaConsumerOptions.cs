using Confluent.Kafka;

namespace AppInfra.Messaging.Kafka.Options;

public sealed class KafkaConsumerOptions
{
    public string BootstrapServers { get; set; }
    public string Topic { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public AutoOffsetReset AutoOffsetReset { get; set; } = AutoOffsetReset.Earliest;
}
