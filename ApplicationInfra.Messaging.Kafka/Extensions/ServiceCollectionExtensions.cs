using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ApplicationInfra.Messaging.Kafka.Extensions;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public void AddKafka(
            IConfiguration configuration,
            Action<MassTransitKafkaConfigurator> configure)
        {
            var configurator = new MassTransitKafkaConfigurator(services, configuration);
            configure(configurator);
            configurator.Configure();
        }
    }
}
