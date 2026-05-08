using ApplicationInfra.Books.Abstract;
using ApplicationInfra.Books.Loggers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ApplicationInfra.Books;

internal sealed class BookRefreshTarget<TKey, TValue> : IBookRefreshTarget
    where TKey : notnull
{
    private readonly ILogger _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly Book<TKey, TValue> _book;
    private readonly IReadOnlyList<IBookRefreshHandler<TKey, TValue>> _bookRefreshHandlers;

    public string Name { get; }
    public TimeSpan RefreshInterval { get; }

    public BookRefreshTarget(
        ILoggerFactory loggerFactory,
        IServiceScopeFactory scopeFactory,
        Book<TKey, TValue> book,
        IEnumerable<IBookRefreshHandler<TKey, TValue>> bookRefreshHandlers,
        TimeSpan refreshInterval,
        string name)
    {
        _logger = loggerFactory.CreateLogger<BookRefreshTarget<TKey, TValue>>();
        _scopeFactory = scopeFactory;
        _book = book;
        _bookRefreshHandlers = [..bookRefreshHandlers];
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
            
            await Task.WhenAll(_bookRefreshHandlers.Select(bookRefreshHandler =>
                    bookRefreshHandler.OnRefreshedAsync(Name, data, cancellationToken)))
                .ConfigureAwait(false);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            Logger.BookRefreshFailed(_logger, exception, Name);
        }
    }
}
