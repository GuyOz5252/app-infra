using ApplicationInfra.Books.Abstract;
using ApplicationInfra.Books.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
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
        services.AddKeyedSingleton<Book<TKey, TValue>>(name);
        services.AddKeyedSingleton<IBook<TKey, TValue>>(name, (serviceProvider, key) =>
            serviceProvider.GetRequiredKeyedService<Book<TKey, TValue>>((string)key!));
        services.TryAddSingleton<IBook<TKey, TValue>>(
            sp => sp.GetRequiredKeyedService<IBook<TKey, TValue>>(name));
        services.AddKeyedScoped<IBookLoader<TKey, TValue>, TLoader>(name);
        services.AddSingleton<IBookRefreshTarget>(sp =>
            new BookRefreshTarget<TKey, TValue>(
                sp.GetRequiredKeyedService<Book<TKey, TValue>>(name),
                sp.GetRequiredService<IServiceScopeFactory>(),
                sp.GetRequiredService<ILoggerFactory>(),
                sp.GetRequiredService<IOptionsMonitor<BookOptions>>().Get(name).RefreshInterval,
                name));
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, BooksOrchestratorHostedService>());
    }
}
