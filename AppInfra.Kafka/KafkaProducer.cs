using AppInfra.Kafka.Abstract;
using AppInfra.Kafka.Options;
using AppInfra.Serialization.Abstract;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AppInfra.Kafka;

public sealed class KafkaProducer<TSerializer> : IKafkaProducer, IAsyncDisposable
    where TSerializer : class, IEventSerializer
{
    private readonly ILogger<KafkaProducer<TSerializer>> _logger;
    private readonly IOptionsSnapshot<KafkaProducerOptions> _optionsSnapshot;
    private readonly string _name;
    private readonly TSerializer _serializer;
    private readonly IProducer<string, byte[]> _producer;
    private int _disposed;

    public KafkaProducer(
        ILogger<KafkaProducer<TSerializer>> logger,
        IOptionsSnapshot<KafkaProducerOptions> optionsSnapshot,
        string name,
        TSerializer serializer)
    {
        _logger = logger;
        _optionsSnapshot = optionsSnapshot;
        _name = name;
        _serializer = serializer;
        _producer = CreateProducer();
    }

    public async Task ProduceAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);

        var kafkaProducerOptions = _optionsSnapshot.Get(_name);

        var bytes = _serializer.Serialize(@event);
        await _producer.ProduceAsync(
                kafkaProducerOptions.Topic,
                new Message<string, byte[]> { Value = bytes },
                cancellationToken)
            .ConfigureAwait(false);
    }

    public ValueTask DisposeAsync()
    {
        if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
        {
            return ValueTask.CompletedTask;
        }

        try
        {
            _producer.Flush(TimeSpan.FromSeconds(10));
        }
        catch (Exception exception)
        {
            _logger.LogDebug(
                exception,
                "Kafka producer flush error. ProducerName={ProducerName}",
                _name);
        }

        _producer.Dispose();
        return ValueTask.CompletedTask;
    }

    private IProducer<string, byte[]> CreateProducer()
    {
        var kafkaProducerOptions = _optionsSnapshot.Get(_name);
        var config = new ProducerConfig
        {
            BootstrapServers = kafkaProducerOptions.BootstrapServers,
            SaslUsername = kafkaProducerOptions.Username,
            SaslPassword = kafkaProducerOptions.Password,
            SecurityProtocol = SecurityProtocol.SaslPlaintext,
            SaslMechanism = SaslMechanism.ScramSha256,
        };

        return new ProducerBuilder<string, byte[]>(config)
            .SetValueSerializer(Serializers.ByteArray)
            .Build();
    }
}
