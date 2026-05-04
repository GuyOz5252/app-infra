using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
    }
}
