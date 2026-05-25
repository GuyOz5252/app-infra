using Serilog.Core;
using Serilog.Events;

namespace ApplicationInfra.Logging.Serilog.Enrichers;

internal sealed class ApplicationInfraLogEnricher : ILogEventEnricher
{
    private readonly LogEventProperty _processName;
    private readonly LogEventProperty _hostName;
    private readonly LogEventProperty _environment;

    public ApplicationInfraLogEnricher(string processName, string hostName, string environment)
    {
        _processName = new LogEventProperty("ProcessName", new ScalarValue(processName));
        _hostName = new LogEventProperty("HostName", new ScalarValue(hostName));
        _environment = new LogEventProperty("Environment", new ScalarValue(environment));
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        logEvent.AddPropertyIfAbsent(_processName);
        logEvent.AddPropertyIfAbsent(_hostName);
        logEvent.AddPropertyIfAbsent(_environment);
    }
}
