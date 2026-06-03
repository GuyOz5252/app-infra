using ApplicationInfra.Books.Abstract;

namespace ApplicationInfra.Books.Tests.Support;

internal sealed class FakeBookRefreshTarget : IBookRefreshTarget
{
    public string Name { get; init; } = "fake";

    public TimeSpan RefreshInterval { get; init; } = Timeout.InfiniteTimeSpan;

    public int RefreshCount { get; private set; }

    public Task RefreshAsync(CancellationToken cancellationToken)
    {
        RefreshCount++;
        return Task.CompletedTask;
    }
}
