namespace ApplicationInfra.Sample.Books;

public sealed class ProductValidator(string productId, decimal maxPrice)
{
    public string ProductId { get; } = productId;

    public bool IsPriceValid(decimal price) => price <= maxPrice;
}
