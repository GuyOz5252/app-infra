using ApplicationInfra.Messaging.Abstractions;
using ApplicationInfra.Messaging.Kafka.MassTransit.Options;
using ApplicationInfra.Messaging.Kafka.MassTransit.Serialization;
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

    // Tracks the first registered entry so Apply() can resolve bootstrap servers for k.Host().
    private (string Name, bool IsProducer)? _firstEntry;

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

        _firstEntry ??= (name, IsProducer: true);

        // rider.AddProducer needs topic and ProducerConfig before DI is built (AddRider runs at
        // service-registration time), so we read from IConfiguration here — the only point at which
        // IOptionsMonitor is not yet available.
        _riderActions.Add(rider =>
        {
            var opts = _configuration
                .GetSection($"Kafka:Producers:{name}")
                .Get<MassTransitKafkaProducerOptions>() ?? new MassTransitKafkaProducerOptions();

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

        _firstEntry ??= (name, IsProducer: false);

        _riderActions.Add(rider => rider.AddConsumer<MassTransitConsumer<TEvent>>());

        // UsingKafka callback runs after DI is built (IRiderRegistrationContext is the service provider),
        // so we can resolve IOptionsMonitor here for full named-options support.
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

    internal void Apply()
    {
        var firstEntry = _firstEntry;
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
                    // Required by MassTransit validation; per-endpoint configs override this for actual connections.
                    k.Host(ResolveFirstBootstrapServers(context, firstEntry));

                    foreach (var action in kafkaEndpointActions)
                    {
                        action(context, k);
                    }
                });
            });
        });
    }

    private static string ResolveFirstBootstrapServers(
        IRiderRegistrationContext context,
        (string Name, bool IsProducer)? firstEntry)
    {
        if (firstEntry is null)
        {
            return "localhost:9092";
        }

        if (firstEntry.Value.IsProducer)
        {
            var opts = context
                .GetRequiredService<IOptionsMonitor<MassTransitKafkaProducerOptions>>()
                .Get(firstEntry.Value.Name);
            return string.IsNullOrWhiteSpace(opts.BootstrapServers) ? "localhost:9092" : opts.BootstrapServers;
        }
        else
        {
            var opts = context
                .GetRequiredService<IOptionsMonitor<MassTransitKafkaConsumerOptions>>()
                .Get(firstEntry.Value.Name);
            return string.IsNullOrWhiteSpace(opts.BootstrapServers) ? "localhost:9092" : opts.BootstrapServers;
        }
    }

    private static ProducerConfig BuildProducerConfig(MassTransitKafkaProducerOptions options)
    {
        var config = new ProducerConfig
        {
            BootstrapServers = options.BootstrapServers,
        };

        if (options.Username is not null)
        {
            config.SaslUsername = options.Username;
            config.SaslPassword = options.Password;
            config.SecurityProtocol = SecurityProtocol.SaslPlaintext;
            config.SaslMechanism = SaslMechanism.Plain;
        }

        return config;
    }

    private static ConsumerConfig BuildConsumerConfig(MassTransitKafkaConsumerOptions options)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = options.BootstrapServers,
            GroupId = options.ConsumerGroup,
        };

        if (options.Username is not null)
        {
            config.SaslUsername = options.Username;
            config.SaslPassword = options.Password;
            config.SecurityProtocol = SecurityProtocol.SaslPlaintext;
            config.SaslMechanism = SaslMechanism.Plain;
        }

        return config;
    }
}
