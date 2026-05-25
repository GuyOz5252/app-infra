using ApplicationInfra.Logging.Serilog.Options;
using Microsoft.Extensions.Options;

namespace ApplicationInfra.Logging.Serilog.Validators;

public sealed class ApplicationInfraLoggingOptionsValidator : IValidateOptions<ApplicationInfraLoggingOptions>
{
    public ValidateOptionsResult Validate(string? name, ApplicationInfraLoggingOptions options)
    {
        return string.IsNullOrWhiteSpace(options.ProcessName)
            ? ValidateOptionsResult.Fail("Logging:ProcessName is required.")
            : ValidateOptionsResult.Success;
    }
}
