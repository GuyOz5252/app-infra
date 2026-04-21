using AppInfra.Kafka.Abstract;
using AppInfra.Serialization.Abstract;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AppInfra.Kafka.Extensions;

public static class KafkaServiceCollectionExtensions
{
    public static void AddKafkaConsumer<TEvent, TEventProcessor, TDeserializer>(
        this IServiceCollection services,
        IConfiguration configuration,
        string name)
        where TEventProcessor : class, IKafkaEventProcessor<TEvent>
        where TDeserializer : class, IEventDeserializer
    {
        services.AddKeyedScoped<TEventProcessor>(name);
        services.Configure<KafkaConsumerOptions>(name, configuration.GetSection($"Kafka:Consumer:{name}"));
        services.AddHostedService(serviceProvider =>
            new KafkaConsumerHostedService<TEvent, TEventProcessor, TDeserializer>(
                serviceProvider
                    .GetRequiredService<ILogger<KafkaConsumerHostedService<TEvent, TEventProcessor, TDeserializer>>>(),
                serviceProvider.GetRequiredService<IOptionsMonitor<KafkaConsumerOptions>>(),
                serviceProvider.GetRequiredService<IServiceScopeFactory>(),
                name,
                serviceProvider.GetRequiredService<TDeserializer>()));
    }
}
