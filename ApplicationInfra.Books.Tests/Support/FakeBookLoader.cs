using ApplicationInfra.Books.Abstract;

namespace ApplicationInfra.Books.Tests.Support;

internal sealed class FakeBookLoader : IBookLoader<string, int>
{
    private readonly Func<CancellationToken, Task<IReadOnlyDictionary<string, int>>> _load;

    public FakeBookLoader(IReadOnlyDictionary<string, int> data)
        : this(_ => Task.FromResult(data))
    {
    }

    public FakeBookLoader(Func<CancellationToken, Task<IReadOnlyDictionary<string, int>>> load)
    {
        _load = load;
    }

    public int LoadCallCount { get; private set; }

    public Task<IReadOnlyDictionary<string, int>> LoadAsync(CancellationToken cancellationToken)
    {
        LoadCallCount++;
        return _load(cancellationToken);
    }
}
