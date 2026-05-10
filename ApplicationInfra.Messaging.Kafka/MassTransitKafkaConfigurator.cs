using ApplicationInfra.Messaging.Abstractions;
using ApplicationInfra.Messaging.Kafka.Options;
using ApplicationInfra.Messaging.Kafka.Serialization;
using ApplicationInfra.Serialization.Abstract;
using Confluent.Kafka;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ApplicationInfra.Messaging.Kafka;

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

    public MassTransitKafkaConfigurator AddConsumer<TEvent, TProcessor, TDeserializer>(string name)
        where TEvent : class
        where TProcessor : class, IEventProcessor<TEvent>
        where TDeserializer : notnull
    {
        _services.Configure<KafkaConsumerOptions>(
            name,
            _configuration.GetSection($"Kafka:Consumers:{name}"));

        _services.AddKeyedScoped<IEventProcessor<TEvent>, TProcessor>(name);

        _services.AddTransient<MassTransitConsumer<TEvent>>(sp =>
            new MassTransitConsumer<TEvent>(sp, name));

        _riderActions.Add(rider => rider.AddConsumer<MassTransitConsumer<TEvent>>());

        _kafkaEndpointActions.Add((context, k) =>
        {
            // TODO: Test using options
            var opts = context
                .GetRequiredService<IOptionsMonitor<KafkaConsumerOptions>>()
                .Get(name);

            k.TopicEndpoint<TEvent>(opts.Topic, BuildConsumerConfig(opts), e =>
            {
                e.ConfigureConsumer<MassTransitConsumer<TEvent>>(context);

                if (typeof(TDeserializer).IsAssignableFrom(typeof(IEventDeserializer)))
                {
                    e.SetValueDeserializer(new ConfluentDeserializerAdapter<TEvent>(
                        context.GetRequiredService<TDeserializer>() as IEventDeserializer
                        ?? throw new Exception("")));
                    return;
                }

                e.SetValueDeserializer(context.GetRequiredService<IDeserializer<TEvent>>());
            });
        });

        return this;
    }

    public MassTransitKafkaConfigurator AddProducer<TEvent, TSerializer>(string name)
        where TEvent : class
        where TSerializer : notnull
    {
        _services.Configure<KafkaProducerOptions>(
            name,
            _configuration.GetSection($"Kafka:Producers:{name}"));

        _services.AddKeyedSingleton<IEventPublisher>(name,
            (sp, _) => new MassTransitEventPublisher<TEvent>(sp.GetRequiredService<ITopicProducer<TEvent>>()));

        _riderActions.Add(rider =>
        {
            var options = _configuration
                              .GetSection($"Kafka:Producers:{name}")
                              .Get<KafkaProducerOptions>()
                          ?? throw new Exception(
                              $"Failed to get kafka producer options from configuration for producer: {name}");

            rider.AddProducer<TEvent>(
                options.Topic,
                BuildProducerConfig(options),
                (context, producer) =>
                {
                    if (typeof(TSerializer).IsAssignableFrom(typeof(IEventSerializer)))
                    {
                        producer.SetValueSerializer(new ConfluentSerializerAdapter<TEvent>(
                            context.GetRequiredService<TSerializer>() as IEventSerializer
                                ?? throw new Exception("")));
                        return;
                    }
                    
                    producer.SetValueSerializer(context.GetRequiredService<ISerializer<TEvent>>());
                });
        });

        return this;
    }

    internal void Apply()
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

    private static ProducerConfig BuildProducerConfig(KafkaProducerOptions options)
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

    private static ConsumerConfig BuildConsumerConfig(KafkaConsumerOptions options)
    {
        return new ConsumerConfig
        {
            BootstrapServers = options.BootstrapServers,
            GroupId = options.ConsumerGroup,
            SaslUsername = options.ConsumerGroup,
            SaslPassword = options.Password,
            SecurityProtocol = SecurityProtocol.SaslPlaintext,
            SaslMechanism = SaslMechanism.Plain
        };
    }
}
