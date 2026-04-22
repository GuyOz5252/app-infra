using System.Text;
using AppInfra.Messaging.Abstractions;
using AppInfra.Messaging.Kafka.Options;
using AppInfra.Serialization.Abstract;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AppInfra.Messaging.Kafka;

public sealed class KafkaProducer<TSerializer> : IEventPublisher, IAsyncDisposable
    where TSerializer : class, IEventSerializer
{
    private readonly ILogger<KafkaProducer<TSerializer>> _logger;
    private readonly KafkaProducerOptions _kafkaProducerOptions;
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
        _kafkaProducerOptions = optionsSnapshot.Get(name);
        _name = name;
        _serializer = serializer;
        _producer = CreateProducer();
    }

    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
    {
        return PublishAsync(@event, null, cancellationToken);
    }

    public async Task PublishAsync<TEvent>(
        TEvent @event,
        PublishMetadata? metadata,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);

        var bytes = _serializer.Serialize(@event);
        var message = new Message<string, byte[]>
        {
            Value = bytes,
            Headers = BuildHeaders(metadata),
        };

        var messageKey = metadata?.Key;
        if (messageKey is not null)
        {
            message.Key = messageKey;
        }

        await _producer
            .ProduceAsync(_kafkaProducerOptions.Topic, message, cancellationToken)
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

    private static Headers? BuildHeaders(PublishMetadata? metadata)
    {
        if (metadata?.Headers is null || metadata.Headers.Count == 0)
        {
            return null;
        }

        var headers = new Headers();
        foreach (var (key, value) in metadata.Headers)
        {
            headers.Add(key, Encoding.UTF8.GetBytes(value));
        }

        return headers;
    }

    private IProducer<string, byte[]> CreateProducer()
    {
        var config = new ProducerConfig
        {
            BootstrapServers = _kafkaProducerOptions.BootstrapServers,
            SaslUsername = _kafkaProducerOptions.Username,
            SaslPassword = _kafkaProducerOptions.Password,
            SecurityProtocol = SecurityProtocol.SaslPlaintext,
            SaslMechanism = SaslMechanism.ScramSha256,
        };

        return new ProducerBuilder<string, byte[]>(config)
            .SetValueSerializer(Serializers.ByteArray)
            .Build();
    }
}
