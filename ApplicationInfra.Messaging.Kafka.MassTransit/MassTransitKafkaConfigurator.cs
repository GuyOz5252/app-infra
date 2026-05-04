using ApplicationInfra.Messaging.Abstractions;
using ApplicationInfra.Messaging.Kafka.MassTransit.Options;
using Confluent.Kafka;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ApplicationInfra.Messaging.Kafka.MassTransit;

public sealed class MassTransitKafkaConfigurator
{
    private readonly IServiceCollection _services;
    private readonly IConfiguration _configuration;
    private readonly List<Action<IRiderRegistrationConfigurator>> _riderActions = [];
    private readonly List<Action<IRiderRegistrationContext, IKafkaFactoryConfigurator>> _kafkaEndpointActions = [];

    internal MassTransitKafkaConfigurator(IServiceCollection services, IConfiguration configuration)
    {
        _services = services;
        _configuration = configuration;
    }

    public void AddProducer<TEvent>(
        string name,
        Action<IRiderRegistrationContext, IKafkaProducerConfigurator<Null, TEvent>>? configure = null)
        where TEvent : class
    {
        _services.Configure<MassTransitKafkaProducerOptions>(
            name,
            _configuration.GetSection($"Kafka:Producers:{name}"));

        _services.AddKeyedSingleton<IEventPublisher, MassTransitEventPublisher<TEvent>>(name);

        _riderActions.Add(rider =>
        {
            var kafkaProducerOptions = _configuration
                .GetSection($"Kafka:Producers:{name}")
                .Get<MassTransitKafkaProducerOptions>() ?? throw new InvalidOperationException(
                $"Kafka producer configuration for '{name}' is missing.");

            rider.AddProducer<TEvent>(
                kafkaProducerOptions.Topic,
                BuildProducerConfig(kafkaProducerOptions),
                (context, config) => configure?.Invoke(context, config));
        });
    }

    public void AddConsumer<TEvent, TProcessor>(
        string name,
        Action<IKafkaTopicReceiveEndpointConfigurator<Ignore, TEvent>>? configure = null)
        where TEvent : class
        where TProcessor : class, IEventProcessor<TEvent>
    {
        _services.Configure<MassTransitKafkaConsumerOptions>(
            name,
            _configuration.GetSection($"Kafka:Consumers:{name}"));

        _services.AddKeyedScoped<IEventProcessor<TEvent>, TProcessor>(name);

        _riderActions.Add(rider => rider.AddConsumer<MassTransitConsumer<TEvent>>());

        _kafkaEndpointActions.Add((context, config) =>
        {
            var kafkaConsumerOptions = context
                .GetRequiredService<IOptionsMonitor<MassTransitKafkaConsumerOptions>>()
                .Get(name);

            config.TopicEndpoint<TEvent>(
                kafkaConsumerOptions.Topic,
                BuildConsumerConfig(kafkaConsumerOptions),
                endpointConfig =>
                {
                    configure?.Invoke(endpointConfig);
                    endpointConfig.ConfigureConsumer<MassTransitConsumer<TEvent>>(context);
                });
        });
    }

    internal void Configure()
    {
        var riderActions = _riderActions.ToList();
        var kafkaEndpointActions = _kafkaEndpointActions.ToList();

        _services.AddMassTransit(x =>
        {
            x.UsingInMemory();

            x.AddRider(rider =>
            {
                foreach (var action in riderActions)
                {
                    action(rider);
                }

                rider.UsingKafka((context, k) =>
                {
                    k.Host("localhost:9092");

                    foreach (var action in kafkaEndpointActions)
                    {
                        action(context, k);
                    }
                });
            });
        });
    }

    private ProducerConfig BuildProducerConfig(MassTransitKafkaProducerOptions options)
    {
        return new ProducerConfig
        {
            BootstrapServers = options.BootstrapServers,
            SaslUsername = options.Username,
            SaslPassword = options.Password,
            SecurityProtocol = SecurityProtocol.SaslPlaintext,
            SaslMechanism = SaslMechanism.Plain
        };
    }

    private ConsumerConfig BuildConsumerConfig(MassTransitKafkaConsumerOptions options)
    {
        return new ConsumerConfig
        {
            BootstrapServers = options.BootstrapServers,
            GroupId = options.ConsumerGroup,
            SaslUsername = options.Username,
            SaslPassword = options.Password,
            SecurityProtocol = SecurityProtocol.SaslPlaintext,
            SaslMechanism = SaslMechanism.Plain
        };
    }
}
