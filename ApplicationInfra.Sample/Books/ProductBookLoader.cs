using ApplicationInfra.Books.Http;
using ApplicationInfra.Books.Http.Options;
using Microsoft.Extensions.Options;

namespace ApplicationInfra.Sample.Books;

public sealed class ProductBookLoader : HttpBookLoader<string, ProductConfig, ProductDto[]>
{
    public ProductBookLoader(
        IHttpClientFactory httpClientFactory,
        IOptionsMonitor<HttpBookLoaderOptions> optionsMonitor,
        string bookName)
        : base(optionsMonitor, httpClientFactory, bookName)
    {
    }

    protected override IReadOnlyDictionary<string, ProductConfig> Map(ProductDto[] response)
    {
        return response.ToDictionary(
            p => p.Id,
            p => new ProductConfig(p.Name, p.Price), StringComparer.OrdinalIgnoreCase);
    }
}
