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
        public void AddBook<TKey, TValue, TLoader>(IConfiguration configuration, string name)
            where TKey : notnull
            where TLoader : class, IBookLoader<TKey, TValue>
        {
            services.Configure<BookOptions>(name, configuration.GetSection($"Books:{name}"));
            AddBookCore<TKey, TValue, TLoader>(services, name);
        }

        public void AddBook<TKey, TValue, TLoader>(string name, Action<BookOptions> configure)
            where TKey : notnull
            where TLoader : class, IBookLoader<TKey, TValue>
        {
            services.Configure(name, configure);
            AddBookCore<TKey, TValue, TLoader>(services, name);
        }
    }

    private static void AddBookCore<TKey, TValue, TLoader>(IServiceCollection services, string name)
        where TKey : notnull
        where TLoader : class, IBookLoader<TKey, TValue>
    {
        services.TryAddSingleton<Book<TKey, TValue>>();
        services.TryAddSingleton<IBook<TKey, TValue>>(sp => sp.GetRequiredService<Book<TKey, TValue>>());
        services.TryAddScoped<TLoader>();
        services.AddHostedService(sp =>
            new BookHostedService<TKey, TValue, TLoader>(
                sp.GetRequiredService<ILogger<BookHostedService<TKey, TValue, TLoader>>>(),
                sp.GetRequiredService<Book<TKey, TValue>>(),
                sp.GetRequiredService<IServiceScopeFactory>(),
                sp.GetRequiredService<IOptionsMonitor<BookOptions>>(),
                name));
    }
}
