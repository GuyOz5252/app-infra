using ApplicationInfra.Books.Abstract;
using ApplicationInfra.Books.Loggers;
using ApplicationInfra.Books.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApplicationInfra.Books;

internal sealed class BookHostedService<TKey, TValue> : BackgroundService
    where TKey : notnull
{
    private readonly ILogger<BookHostedService<TKey, TValue>> _logger;
    private readonly Book<TKey, TValue> _book;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly string _name;
    private readonly BookOptions _options;

    public BookHostedService(
        ILogger<BookHostedService<TKey, TValue>> logger,
        Book<TKey, TValue> book,
        IServiceScopeFactory serviceScopeFactory,
        IOptionsMonitor<BookOptions> optionsMonitor,
        string name)
    {
        _logger = logger;
        _book = book;
        _serviceScopeFactory = serviceScopeFactory;
        _name = name;
        _options = optionsMonitor.Get(name);
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await RefreshAsync(cancellationToken).ConfigureAwait(false);
        await base.StartAsync(cancellationToken).ConfigureAwait(false);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_options.RefreshInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            await RefreshAsync(stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        try
        {
            Logger.BookRefreshStarted(_logger, _name);
            using var scope = _serviceScopeFactory.CreateScope();
            var loader = scope.ServiceProvider.GetRequiredKeyedService<IBookLoader<TKey, TValue>>(_name);
            var data = await loader.LoadAsync(cancellationToken).ConfigureAwait(false);
            _book.Refresh(data);
            Logger.BookRefreshCompleted(_logger, _name, data.Count);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            Logger.BookRefreshFailed(_logger, exception, _name);
        }
    }
}
