using ApplicationInfra.Books.Abstract;

namespace ApplicationInfra.Books.Tests.Support;

internal sealed class WidgetBookLoader : IBookLoader<string, int>
{
    public Task<IReadOnlyDictionary<string, int>> LoadAsync(CancellationToken cancellationToken)
    {
        IReadOnlyDictionary<string, int> data = new Dictionary<string, int> { ["widget"] = 3 };
        return Task.FromResult(data);
    }
}
