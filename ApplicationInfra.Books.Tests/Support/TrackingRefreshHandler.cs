using ApplicationInfra.Books.Abstract;

namespace ApplicationInfra.Books.Tests.Support;

internal sealed class TrackingRefreshHandler : IBookRefreshHandler<string, int>
{
    public int CallCount { get; private set; }
    public string? LastBookName { get; private set; }
    public IReadOnlyDictionary<string, int>? LastData { get; private set; }

    public Task OnRefreshedAsync(
        string bookName,
        IReadOnlyDictionary<string, int> data,
        CancellationToken cancellationToken)
    {
        CallCount++;
        LastBookName = bookName;
        LastData = data;
        return Task.CompletedTask;
    }
}
