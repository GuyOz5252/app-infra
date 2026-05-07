using ApplicationInfra.Books.Abstract;
using ApplicationInfra.Books.Loggers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ApplicationInfra.Books;

internal sealed class BookRefreshTarget<TKey, TValue> : IBookRefreshTarget
    where TKey : notnull
{
    private readonly Book<TKey, TValue> _book;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger _logger;

    public string Name { get; }
    public TimeSpan RefreshInterval { get; }

    public BookRefreshTarget(
        Book<TKey, TValue> book,
        IServiceScopeFactory scopeFactory,
        ILoggerFactory loggerFactory,
        TimeSpan refreshInterval,
        string name)
    {
        _book = book;
        _scopeFactory = scopeFactory;
        _logger = loggerFactory.CreateLogger<BookRefreshTarget<TKey, TValue>>();
        RefreshInterval = refreshInterval;
        Name = name;
    }

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        try
        {
            Logger.BookRefreshStarted(_logger, Name);
            using var scope = _scopeFactory.CreateScope();
            var loader = scope.ServiceProvider.GetRequiredKeyedService<IBookLoader<TKey, TValue>>(Name);
            var data = await loader.LoadAsync(cancellationToken).ConfigureAwait(false);
            _book.Refresh(data);
            Logger.BookRefreshCompleted(_logger, Name, data.Count);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            Logger.BookRefreshFailed(_logger, exception, Name);
        }
    }
}
