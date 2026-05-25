using Microsoft.AspNetCore.Builder;

namespace ApplicationInfra.Logging.Serilog.Extensions;

public static class WebApplicationBuilderExtensions
{
    extension(WebApplicationBuilder builder)
    {
        public void AddApplicationInfraSerilog()
        {
            builder.Services.AddApplicationInfraSerilog(builder.Configuration, builder.Environment);
        }
    }
}
