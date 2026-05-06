using ApplicationInfra.Messaging.Kafka.MassTransit.Serialization;
using Confluent.Kafka;
using Google.Protobuf;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ApplicationInfra.Messaging.Kafka.MassTransit.Extensions;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public void AddMassTransitKafka(
            IConfiguration configuration,
            Action<MassTransitKafkaConfigurator> configure)
        {
            var configurator = new MassTransitKafkaConfigurator(services, configuration);
            configure(configurator);
            configurator.Configure();
        }

        public void AddConfluentProtobufSerialization<T>() where T : IMessage<T>, new()
        {
            services.TryAddSingleton<ISerializer<T>, ConfluentProtobufSerializer<T>>();
            services.TryAddSingleton<IDeserializer<T>, ConfluentProtobufDeserializer<T>>();
        }
    }
}
