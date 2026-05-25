namespace ApplicationInfra.Logging.Serilog.Internal;

internal static class RollingFilePathResolver
{
    public static string Resolve(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return FileSinkDefaults.DefaultFilePath;
        }

        var extension = Path.GetExtension(filePath);
        if (string.IsNullOrEmpty(extension))
        {
            return filePath + "-.log";
        }

        var basePath = filePath[..^extension.Length];
        if (basePath.EndsWith('-'))
        {
            return filePath;
        }

        return basePath + "-" + extension;
    }
}
