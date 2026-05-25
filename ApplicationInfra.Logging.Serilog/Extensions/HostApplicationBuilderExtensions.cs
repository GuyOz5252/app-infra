using Microsoft.Extensions.Hosting;

namespace ApplicationInfra.Logging.Serilog.Extensions;

public static class HostApplicationBuilderExtensions
{
    extension(HostApplicationBuilder builder)
    {
        public void AddApplicationInfraSerilog()
        {
            builder.Services.AddApplicationInfraSerilog(builder.Configuration, builder.Environment);
        }
    }
}
