namespace ApplicationInfra.Logging.Serilog.Options;

public sealed class ApplicationInfraLoggingOptions
{
    public required string ProcessName { get; set; }
    public string? Environment { get; set; }
    public string? FilePath { get; set; }
}
