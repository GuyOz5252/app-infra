using AppInfra.Messaging.Kafka.Extensions;
using AppInfra.Messaging.Abstractions;
using AppInfra.Messaging.Kafka.Options;
using AppInfra.Serialization.Abstract;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AppInfra.Messaging.Kafka;

public sealed class KafkaConsumerHostedService<TEvent, THandler, TDeserializer> : BackgroundService
    where THandler : class, IEventProcessor<TEvent>
    where TDeserializer : class, IEventDeserializer
{
    private readonly ILogger<KafkaConsumerHostedService<TEvent, THandler, TDeserializer>> _logger;
    private readonly KafkaConsumerOptions _kafkaConsumerOptions;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly string _name;
    private readonly TDeserializer _deserializer;

    public KafkaConsumerHostedService(
        ILogger<KafkaConsumerHostedService<TEvent, THandler, TDeserializer>> logger,
        IOptionsSnapshot<KafkaConsumerOptions> optionsSnapshot,
        IServiceScopeFactory serviceScopeFactory,
        string name,
        TDeserializer deserializer)
    {
        _logger = logger;
        _kafkaConsumerOptions = optionsSnapshot.Get(name);
        _serviceScopeFactory = serviceScopeFactory;
        _name = name;
        _deserializer = deserializer;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _kafkaConsumerOptions.BootstrapServers,
            GroupId = _kafkaConsumerOptions.Username,
            SaslUsername = _kafkaConsumerOptions.Username,
            SaslPassword = _kafkaConsumerOptions.Password,
            SecurityProtocol = SecurityProtocol.SaslPlaintext,
            SaslMechanism = SaslMechanism.ScramSha256,
            AutoOffsetReset = _kafkaConsumerOptions.AutoOffsetReset,
            EnableAutoCommit = true,
        };

        using var consumer = new ConsumerBuilder<string, byte[]>(config)
            .SetValueDeserializer(Deserializers.ByteArray)
            .Build();

        consumer.Subscribe(_kafkaConsumerOptions.Topic);
        _logger.LogInformation(
            "Kafka consumer {ConsumerName} subscribed. Topic={Topic}, GroupId={GroupId}",
            _name,
            _kafkaConsumerOptions.Topic,
            _kafkaConsumerOptions.Username);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                ConsumeResult<string, byte[]>? result;
                try
                {
                    result = consumer.Consume(stoppingToken);
                }
                catch (ConsumeException consumeException)
                {
                    _logger.LogError(consumeException, "Kafka consume error. ConsumerName={ConsumerName}", _name);
                    continue;
                }

                if (result is null || result.IsPartitionEOF)
                {
                    continue;
                }

                try
                {
                    var @event = _deserializer.Deserialize<TEvent>(result.Message.Value);
                    var context = result.ToEventContext();

                    using var scope = _serviceScopeFactory.CreateScope();
                    var eventProcessor =
                        scope.ServiceProvider.GetRequiredKeyedService<IEventProcessor<TEvent>>(_name);
                    await eventProcessor.ProcessEventAsync(@event, context, stoppingToken).ConfigureAwait(false);

                    consumer.Commit(result);
                }
                catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogError(
                        exception,
                        "Failed to process Kafka event. ConsumerName={ConsumerName}, Destination={Destination}, Partition={Partition}, Offset={Offset}",
                        _name,
                        result.Topic,
                        result.Partition.Value,
                        result.Offset.Value);
                }
            }
        }
        finally
        {
            try
            {
                consumer.Close();
            }
            catch (Exception exception)
            {
                _logger.LogDebug(exception, "Error closing Kafka consumer.");
            }
        }
    }
}
