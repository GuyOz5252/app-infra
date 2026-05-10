using Microsoft.Extensions.Logging;

namespace ApplicationInfra.Books.Loggers;

internal static partial class Logger
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Book '{BookName}' refresh started.")]
    internal static partial void BookRefreshStarted(ILogger logger, string bookName);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Book '{BookName}' refresh completed. Entries={EntryCount}")]
    internal static partial void BookRefreshCompleted(ILogger logger, string bookName, int entryCount);

    [LoggerMessage(Level = LogLevel.Error, Message = "Book '{BookName}' refresh failed.")]
    internal static partial void BookRefreshFailed(ILogger logger, Exception exception, string bookName);
}
