using ApplicationInfra.Books.Abstract;
using ApplicationInfra.Books.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApplicationInfra.Books.Extensions;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers a book whose options are bound from <c>Books:{name}</c> in <paramref name="configuration"/>.
        /// Inject the book as <c>[FromKeyedServices("name")] IBook&lt;TKey, TValue&gt;</c>.
        /// </summary>
        public void AddBook<TKey, TValue, TLoader>(IConfiguration configuration, string name)
            where TKey : notnull
            where TLoader : class, IBookLoader<TKey, TValue>
        {
            services.Configure<BookOptions>(name, configuration.GetSection($"Books:{name}"));
            AddBookCore<TKey, TValue, TLoader>(services, name);
        }

        /// <summary>
        /// Registers a book configured via an inline delegate.
        /// Inject the book as <c>[FromKeyedServices("name")] IBook&lt;TKey, TValue&gt;</c>.
        /// </summary>
        public void AddBook<TKey, TValue, TLoader>(string name, Action<BookOptions> configure)
            where TKey : notnull
            where TLoader : class, IBookLoader<TKey, TValue>
        {
            services.Configure(name, configure);
            AddBookCore<TKey, TValue, TLoader>(services, name);
        }
    }

    internal static void AddBookCore<TKey, TValue, TLoader>(IServiceCollection services, string name)
        where TKey : notnull
        where TLoader : class, IBookLoader<TKey, TValue>
    {
        // The concrete book instance is keyed so multiple books of the same <TKey, TValue>
        // can coexist. The hosted service holds a direct reference to its own instance.
        services.AddKeyedSingleton<Book<TKey, TValue>>(name);

        // Public interface — always keyed; first book of each <TKey, TValue> pair also gets
        // an unkeyed registration as a convenience default.
        services.AddKeyedSingleton<IBook<TKey, TValue>>(
            name,
            (sp, key) => sp.GetRequiredKeyedService<Book<TKey, TValue>>((string)key!));
        services.TryAddSingleton<IBook<TKey, TValue>>(
            sp => sp.GetRequiredKeyedService<IBook<TKey, TValue>>(name));

        // Loader is keyed so the hosted service resolves the right one even when multiple
        // books share the same <TKey, TValue> types.
        services.AddKeyedScoped<IBookLoader<TKey, TValue>, TLoader>(name);

        services.AddHostedService(sp =>
            new BookHostedService<TKey, TValue>(
                sp.GetRequiredService<ILogger<BookHostedService<TKey, TValue>>>(),
                sp.GetRequiredKeyedService<Book<TKey, TValue>>(name),
                sp.GetRequiredService<IServiceScopeFactory>(),
                sp.GetRequiredService<IOptionsMonitor<BookOptions>>(),
                name));
    }
}
