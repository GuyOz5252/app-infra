using AppInfra.Serialization.Abstract;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AppInfra.Kafka;

public sealed class KafkaConsumerHostedService<TEvent, THandler, TDeserializer> : BackgroundService
    where THandler : class, IKafkaEventProcessor<TEvent>
    where TDeserializer : class, IEventDeserializer
{
    private readonly ILogger<KafkaConsumerHostedService<TEvent, THandler, TDeserializer>> _logger;
    private readonly IOptionsMonitor<KafkaConsumerOptions> _optionsMonitor;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly string _name;
    private readonly TDeserializer _deserializer;

    public KafkaConsumerHostedService(
        ILogger<KafkaConsumerHostedService<TEvent, THandler, TDeserializer>> logger,
        IOptionsMonitor<KafkaConsumerOptions> optionsMonitor,
        IServiceScopeFactory serviceScopeFactory,
        string name,
        TDeserializer deserializer)
    {
        _logger = logger;
        _optionsMonitor = optionsMonitor;
        _serviceScopeFactory = serviceScopeFactory;
        _name = name;
        _deserializer = deserializer;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var kafkaConsumerOptions = _optionsMonitor.Get(_name);

        var config = new ConsumerConfig
        {
            BootstrapServers = kafkaConsumerOptions.BootstrapServers,
            GroupId = kafkaConsumerOptions.Username,
            SaslUsername = kafkaConsumerOptions.Username,
            SaslPassword = kafkaConsumerOptions.Password,
            SecurityProtocol = SecurityProtocol.SaslPlaintext,
            SaslMechanism = SaslMechanism.ScramSha256,
            AutoOffsetReset = kafkaConsumerOptions.AutoOffsetReset,
            EnableAutoCommit = true,
        };

        using var consumer = new ConsumerBuilder<string, byte[]>(config)
            .SetValueDeserializer(Deserializers.ByteArray)
            .Build();

        consumer.Subscribe(kafkaConsumerOptions.Topic);
        _logger.LogInformation(
            "Kafka consumer {ConsumerName} subscribed. Topic={Topic}, GroupId={GroupId}",
            _name,
            kafkaConsumerOptions.Topic,
            kafkaConsumerOptions.Username);

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

                    using var scope = _serviceScopeFactory.CreateScope();
                    var eventProcessor =
                        scope.ServiceProvider.GetRequiredKeyedService<IKafkaEventProcessor<TEvent>>(_name);
                    await eventProcessor.ProcessEventAsync(@event, stoppingToken).ConfigureAwait(false);

                    consumer.Commit(result);
                }
                catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogError(
                        exception,
                        "Failed to process Kafka event. ConsumerName={ConsumerName}, Topic={Topic}, Partition={Partition}, Offset={Offset}",
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
