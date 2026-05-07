using ApplicationInfra.Books.Abstract;
using Microsoft.Extensions.Hosting;

namespace ApplicationInfra.Books;

internal sealed class BooksOrchestratorHostedService : BackgroundService
{
    private readonly IEnumerable<IBookRefreshTarget> _targets;

    public BooksOrchestratorHostedService(IEnumerable<IBookRefreshTarget> targets)
    {
        _targets = targets;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await Task.WhenAll(_targets.Select(t => t.RefreshAsync(cancellationToken)))
            .ConfigureAwait(false);

        await base.StartAsync(cancellationToken).ConfigureAwait(false);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.WhenAll(_targets.Select(t => RunRefreshLoop(t, stoppingToken)));
    }

    private static async Task RunRefreshLoop(IBookRefreshTarget target, CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(target.RefreshInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            await target.RefreshAsync(stoppingToken).ConfigureAwait(false);
        }
    }
}
