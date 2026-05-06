using ApplicationInfra.Books.Abstract;

namespace ApplicationInfra.Sample.Books;

public sealed class ProductBookLoader : IBookLoader<string, ProductConfig>
{
    public Task<IReadOnlyDictionary<string, ProductConfig>> LoadAsync(CancellationToken cancellationToken)
    {
        // In a real service this would query a database or internal API.
        IReadOnlyDictionary<string, ProductConfig> data = new Dictionary<string, ProductConfig>(StringComparer.OrdinalIgnoreCase)
        {
            ["prod-001"] = new ProductConfig("Widget", 9.99m),
            ["prod-002"] = new ProductConfig("Gadget", 29.99m),
            ["prod-003"] = new ProductConfig("Doohickey", 4.49m),
        };

        return Task.FromResult(data);
    }
}
