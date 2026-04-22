using AppInfra.Kafka.Options;
using AppInfra.Messaging.Abstractions;
using AppInfra.Serialization.Abstract;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AppInfra.Kafka.Extensions;

public static class KafkaServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public void AddKafkaProducer<TSerializer>(IConfiguration configuration,
            string name)
            where TSerializer : class, IEventSerializer
        {
            services.Configure<KafkaProducerOptions>(name, configuration.GetSection($"Kafka:Producer:{name}"));
            services.TryAddSingleton<TSerializer>();
            services.AddKeyedSingleton<IEventPublisher>(
                name,
                (serviceProvider, _) => new KafkaProducer<TSerializer>(
                    serviceProvider.GetRequiredService<ILogger<KafkaProducer<TSerializer>>>(),
                    serviceProvider.GetRequiredService<IOptionsSnapshot<KafkaProducerOptions>>(),
                    name,
                    serviceProvider.GetRequiredService<TSerializer>()));
        }

        public void AddKafkaConsumer<TEvent, TEventProcessor, TDeserializer>(
            IConfiguration configuration,
            string name)
            where TEventProcessor : class, IEventProcessor<TEvent>
            where TDeserializer : class, IEventDeserializer
        {
            services.AddKeyedScoped<TEventProcessor>(name);
            services.Configure<KafkaConsumerOptions>(name, configuration.GetSection($"Kafka:Consumer:{name}"));
            services.AddHostedService(serviceProvider =>
                new KafkaConsumerHostedService<TEvent, TEventProcessor, TDeserializer>(
                    serviceProvider
                        .GetRequiredService<ILogger<KafkaConsumerHostedService<TEvent, TEventProcessor, TDeserializer>>>(),
                    serviceProvider.GetRequiredService<IOptionsSnapshot<KafkaConsumerOptions>>(),
                    serviceProvider.GetRequiredService<IServiceScopeFactory>(),
                    name,
                    serviceProvider.GetRequiredService<TDeserializer>()));
        }
    }
}
