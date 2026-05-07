using ApplicationInfra.Books.Abstract;
using ApplicationInfra.Books.Extensions;
using ApplicationInfra.Books.Http.Options;
using ApplicationInfra.Books.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ApplicationInfra.Books.Http.Extensions;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers an HTTP-backed book whose options (<see cref="BookOptions"/> and
        /// <see cref="HttpBookLoaderOptions"/>) are bound from <c>Books:{name}</c> in
        /// <paramref name="configuration"/>.
        /// Inject the book as <c>[FromKeyedServices("name")] IBook&lt;TKey, TValue&gt;</c>.
        /// </summary>
        public void AddHttpBook<TKey, TValue, TLoader>(IConfiguration configuration, string name)
            where TKey : notnull
            where TLoader : class, IBookLoader<TKey, TValue>
        {
            services.Configure<HttpBookLoaderOptions>(name, configuration.GetSection($"Books:{name}"));
            services.AddHttpClient();
            services.AddBook<TKey, TValue, TLoader>(configuration, name);
        }

        /// <summary>
        /// Registers an HTTP-backed book configured via inline delegates.
        /// Inject the book as <c>[FromKeyedServices("name")] IBook&lt;TKey, TValue&gt;</c>.
        /// </summary>
        public void AddHttpBook<TKey, TValue, TLoader>(
            string name,
            Action<BookOptions> configureBook,
            Action<HttpBookLoaderOptions> configureLoader)
            where TKey : notnull
            where TLoader : class, IBookLoader<TKey, TValue>
        {
            services.Configure(name, configureLoader);
            services.AddHttpClient();
            services.AddBook<TKey, TValue, TLoader>(name, configureBook);
        }
    }
}
