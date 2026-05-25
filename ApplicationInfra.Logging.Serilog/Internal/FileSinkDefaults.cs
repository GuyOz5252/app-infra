namespace ApplicationInfra.Logging.Serilog.Internal;

internal static class FileSinkDefaults
{
    public const string DefaultFilePath = "Log-.log";
    public const int RetainedFileCountLimit = 31;
    public const long FileSizeLimitBytes = 104_857_600;
}
