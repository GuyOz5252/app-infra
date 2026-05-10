using System.Net.Http.Json;
using ApplicationInfra.Books.Abstract;
using ApplicationInfra.Books.Http.Options;
using Microsoft.Extensions.Options;

namespace ApplicationInfra.Books.Http;

/// <summary>
/// Base class for a book loader that fetches data from an HTTP endpoint via a GET request.
/// The URL and optional named client are configured through <see cref="HttpBookLoaderOptions"/>
/// under the <c>Books:{bookName}</c> configuration section.
/// The endpoint must return JSON that deserializes to <typeparamref name="TResponse"/>.
/// </summary>
/// <typeparam name="TKey">The book's key type.</typeparam>
/// <typeparam name="TValue">The book's value type.</typeparam>
/// <typeparam name="TResponse">The JSON response type returned by the endpoint.</typeparam>
/// <example>
/// <code>
/// // appsettings.json
/// // "Books": { "Products": { "Url": "https://api.internal/products" } }
///
/// public sealed class ProductBookLoader : HttpBookLoader&lt;string, ProductConfig, ProductDto[]&gt;
/// {
///     public ProductBookLoader(
///         IHttpClientFactory httpClientFactory,
///         IOptionsMonitor&lt;HttpBookLoaderOptions&gt; optionsMonitor)
///         : base(optionsMonitor, httpClientFactory, "Products")
///     {
///     }
///
///     protected override IReadOnlyDictionary&lt;string, ProductConfig&gt; Map(ProductDto[] response)
///     {
///         return response.ToDictionary(p =&gt; p.Id, p =&gt; new ProductConfig(p.Name, p.Price));
///     }
/// }
/// </code>
/// </example>
public abstract class HttpBookLoader<TKey, TValue, TResponse> : IBookLoader<TKey, TValue>
    where TKey : notnull
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly HttpBookLoaderOptions _options;

    protected HttpBookLoader(
        IOptionsMonitor<HttpBookLoaderOptions> optionsMonitor,
        IHttpClientFactory httpClientFactory,
        string bookName)
    {
        _options = optionsMonitor.Get(bookName);
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>Maps the deserialized HTTP response to the book's dictionary.</summary>
    protected abstract IReadOnlyDictionary<TKey, TValue> Map(TResponse response);

    public virtual async Task<IReadOnlyDictionary<TKey, TValue>> LoadAsync(CancellationToken cancellationToken)
    {
        using var client = _httpClientFactory.CreateClient(_options.HttpClientName);
        var response = await client
            .GetFromJsonAsync<TResponse>(_options.Url, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException(
                $"HTTP book loader received a null response from '{_options.Url}'.");

        return Map(response);
    }
}
