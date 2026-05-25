using ApplicationInfra.Logging.Serilog.Internal;
using ApplicationInfra.Logging.Serilog.Options;
using ApplicationInfra.Logging.Serilog.Validators;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;

namespace ApplicationInfra.Logging.Serilog.Extensions;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public void AddApplicationInfraSerilog(IConfiguration configuration, IHostEnvironment environment)
        {
            services.AddLogging(loggingBuilder => loggingBuilder.ClearProviders());
            services.AddOptions<ApplicationInfraLoggingOptions>()
                .Bind(configuration.GetSection("Logging"))
                .ValidateOnStart();
            services.AddSingleton<IValidateOptions<ApplicationInfraLoggingOptions>, ApplicationInfraLoggingOptionsValidator>();

            services.AddSerilog((serviceProvider, loggerConfiguration) =>
            {
                SerilogConfigurationBuilder.Configure(
                    loggerConfiguration,
                    configuration,
                    serviceProvider.GetRequiredService<IOptions<ApplicationInfraLoggingOptions>>().Value,
                    environment);
            });
        }
    }
}
