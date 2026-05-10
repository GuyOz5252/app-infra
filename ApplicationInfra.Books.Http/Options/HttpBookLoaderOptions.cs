namespace ApplicationInfra.Books.Http.Options;

public sealed class HttpBookLoaderOptions
{
    /// <summary>The URL to GET on each refresh cycle.</summary>
    public required string Url { get; set; }

    /// <summary>
    /// Name of the <see cref="System.Net.Http.HttpClient"/> to use from
    /// <see cref="System.Net.Http.IHttpClientFactory"/>. Leave empty to use the default unnamed client.
    /// </summary>
    public string HttpClientName { get; set; } = string.Empty;
}
