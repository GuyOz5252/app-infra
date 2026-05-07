using ApplicationInfra.Books.Abstract;
using ApplicationInfra.Books.Extensions;
using ApplicationInfra.Books.Options;
using ApplicationInfra.Books.Oracle.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ApplicationInfra.Books.Oracle.Extensions;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers an Oracle-backed book whose options (<see cref="BookOptions"/> and
        /// <see cref="OracleBookLoaderOptions"/>) are bound from <c>Books:{name}</c> in
        /// <paramref name="configuration"/>.
        /// Inject the book as <c>[FromKeyedServices("name")] IBook&lt;TKey, TValue&gt;</c>.
        /// </summary>
        public void AddOracleBook<TKey, TValue, TLoader>(IConfiguration configuration, string name)
            where TKey : notnull
            where TLoader : class, IBookLoader<TKey, TValue>
        {
            services.Configure<OracleBookLoaderOptions>(name, configuration.GetSection($"Books:{name}"));
            services.AddBook<TKey, TValue, TLoader>(configuration, name);
        }

        /// <summary>
        /// Registers an Oracle-backed book configured via inline delegates.
        /// Inject the book as <c>[FromKeyedServices("name")] IBook&lt;TKey, TValue&gt;</c>.
        /// </summary>
        public void AddOracleBook<TKey, TValue, TLoader>(
            string name,
            Action<BookOptions> configureBook,
            Action<OracleBookLoaderOptions> configureLoader)
            where TKey : notnull
            where TLoader : class, IBookLoader<TKey, TValue>
        {
            services.Configure(name, configureLoader);
            services.AddBook<TKey, TValue, TLoader>(name, configureBook);
        }
    }
}
