using ApplicationInfra.Books.Abstract;

namespace ApplicationInfra.Sample.Books;

public sealed class ProductValidatorRegistry : IBookRefreshHandler<string, ProductConfig>
{
    private IReadOnlyList<ProductValidator> _validators = [];

    public IReadOnlyList<ProductValidator> GetAll() =>
        Volatile.Read(ref _validators);

    public Task OnRefreshedAsync(string bookName, IReadOnlyDictionary<string, ProductConfig> data, CancellationToken cancellationToken)
    {
        var validators = data
            .Select(kvp => new ProductValidator(kvp.Key, kvp.Value.Price))
            .ToList();

        Volatile.Write(ref _validators, validators);
        return Task.CompletedTask;
    }
}
