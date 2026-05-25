using System.Globalization;
using ApplicationInfra.Logging.Serilog.Enrichers;
using ApplicationInfra.Logging.Serilog.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Serilog.Settings.Configuration;

namespace ApplicationInfra.Logging.Serilog.Internal;

internal static class SerilogConfigurationBuilder
{
    public static void Configure(
        LoggerConfiguration loggerConfiguration,
        IConfiguration configuration,
        ApplicationInfraLoggingOptions options,
        IHostEnvironment hostEnvironment)
    {
        ArgumentNullException.ThrowIfNull(loggerConfiguration);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(hostEnvironment);

        var serilogSection = configuration.GetSection("Serilog");
        var hasWriteTo = serilogSection.GetSection("WriteTo").GetChildren().Any();
        var hasMinimumLevel = serilogSection.GetSection("MinimumLevel").Exists();

        loggerConfiguration.Enrich.With(new ApplicationInfraLogEnricher(
                options.ProcessName,
                ResolveHostName(),
                ResolveEnvironment(options, hostEnvironment)))
            .Enrich.FromLogContext();

        if (serilogSection.Exists())
        {
            loggerConfiguration.ReadFrom.Configuration(configuration);
        }

        if (!hasMinimumLevel)
        {
            ApplyLoggingLogLevelBridge(loggerConfiguration, configuration);
        }

        if (!hasWriteTo)
        {
            AddDefaultSinks(loggerConfiguration, options.FilePath);
        }
    }

    private static string ResolveHostName()
    {
        var hostName = Environment.GetEnvironmentVariable("HOSTNAME");
        return !string.IsNullOrWhiteSpace(hostName) ? hostName : Environment.MachineName;
    }

    private static string ResolveEnvironment(ApplicationInfraLoggingOptions options, IHostEnvironment hostEnvironment)
    {
        if (!string.IsNullOrWhiteSpace(options.Environment))
        {
            return options.Environment;
        }

        return hostEnvironment.EnvironmentName;
    }

    private static void ApplyLoggingLogLevelBridge(LoggerConfiguration loggerConfiguration,
        IConfiguration configuration)
    {
        var logLevelSection = configuration.GetSection("Logging:LogLevel");
        if (!logLevelSection.Exists())
        {
            loggerConfiguration.MinimumLevel.Information();
            return;
        }

        foreach (var child in logLevelSection.GetChildren())
        {
            if (!TryMapLogLevel(child.Value, out var serilogLevel))
            {
                continue;
            }

            if (string.Equals(child.Key, "Default", StringComparison.OrdinalIgnoreCase))
            {
                loggerConfiguration.MinimumLevel.Is(serilogLevel);
            }
            else
            {
                loggerConfiguration.MinimumLevel.Override(child.Key, serilogLevel);
            }
        }
    }

    private static bool TryMapLogLevel(string? value, out LogEventLevel serilogLevel)
    {
        serilogLevel = LogEventLevel.Information;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (!Enum.TryParse<Microsoft.Extensions.Logging.LogLevel>(value, ignoreCase: true, out var melLevel))
        {
            return false;
        }

        serilogLevel = melLevel switch
        {
            Microsoft.Extensions.Logging.LogLevel.Trace => LogEventLevel.Verbose,
            Microsoft.Extensions.Logging.LogLevel.Debug => LogEventLevel.Debug,
            Microsoft.Extensions.Logging.LogLevel.Information => LogEventLevel.Information,
            Microsoft.Extensions.Logging.LogLevel.Warning => LogEventLevel.Warning,
            Microsoft.Extensions.Logging.LogLevel.Error => LogEventLevel.Error,
            Microsoft.Extensions.Logging.LogLevel.Critical => LogEventLevel.Fatal,
            Microsoft.Extensions.Logging.LogLevel.None => LogEventLevel.Fatal,
            _ => LogEventLevel.Information,
        };

        return true;
    }

    private static void AddDefaultSinks(LoggerConfiguration loggerConfiguration, string? filePath)
    {
        var rollingPath = RollingFilePathResolver.Resolve(filePath);

        loggerConfiguration
            .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
            .WriteTo.File(
                formatter: new CompactJsonFormatter(),
                path: rollingPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: FileSinkDefaults.RetainedFileCountLimit,
                rollOnFileSizeLimit: true,
                fileSizeLimitBytes: FileSinkDefaults.FileSizeLimitBytes);
    }
}
