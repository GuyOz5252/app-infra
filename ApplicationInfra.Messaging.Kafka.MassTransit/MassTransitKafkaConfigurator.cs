using ApplicationInfra.Messaging.Abstractions;
using ApplicationInfra.Messaging.Kafka.MassTransit.Serialization;
using ApplicationInfra.Messaging.Kafka.MassTransit.Options;
using ApplicationInfra.Serialization.Abstract;
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

    public MassTransitKafkaConfigurator AddProducer<TEvent, TSerializer>(string name)
        where TEvent : class
        where TSerializer : class, IEventSerializer
    {
        _services.Configure<MassTransitKafkaProducerOptions>(
            name,
            _configuration.GetSection($"Kafka:Producers:{name}"));

        _services.AddKeyedSingleton<IEventPublisher>(name,
            (sp, _) => new MassTransitEventPublisher<TEvent>(sp.GetRequiredService<ITopicProducer<TEvent>>()));
        
        _riderActions.Add(rider =>
        {
            var opts = _configuration
                .GetSection($"Kafka:Producers:{name}")
                .Get<MassTransitKafkaProducerOptions>() ?? throw new Exception(); // TODO: Exception

            rider.AddProducer<TEvent>(opts.Topic, BuildProducerConfig(opts), (riderCtx, p) =>
                p.SetValueSerializer(new ConfluentSerializerAdapter<TEvent>(
                    riderCtx.GetRequiredService<TSerializer>())));
        });

        return this;
    }

    public MassTransitKafkaConfigurator AddConsumer<TEvent, TProcessor, TDeserializer>(string name)
        where TEvent : class
        where TProcessor : class, IEventProcessor<TEvent>
        where TDeserializer : class, IEventDeserializer
    {
        _services.Configure<MassTransitKafkaConsumerOptions>(
            name,
            _configuration.GetSection($"Kafka:Consumers:{name}"));

        _services.AddKeyedScoped<IEventProcessor<TEvent>, TProcessor>(name);

        _services.AddTransient<MassTransitConsumer<TEvent>>(sp =>
            new MassTransitConsumer<TEvent>(sp, name));

        _riderActions.Add(rider => rider.AddConsumer<MassTransitConsumer<TEvent>>());

        _kafkaEndpointActions.Add((context, k) =>
        {
            var opts = context
                .GetRequiredService<IOptionsMonitor<MassTransitKafkaConsumerOptions>>()
                .Get(name);

            k.TopicEndpoint<TEvent>(opts.Topic, BuildConsumerConfig(opts), e =>
            {
                e.SetValueDeserializer(new ConfluentDeserializerAdapter<TEvent>(
                    context.GetRequiredService<TDeserializer>()));
                e.ConfigureConsumer<MassTransitConsumer<TEvent>>(context);
            });
        });

        return this;
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
