using ApplicationInfra.Books.Abstract;
using ApplicationInfra.Books.Extensions;
using ApplicationInfra.Books.Tests.Support;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ApplicationInfra.Books.Tests;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddBook_RegistersKeyedBookLoaderAndRefreshTarget()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBook<string, int, WidgetBookLoader>(
            "books",
            options => options.RefreshInterval = TimeSpan.FromSeconds(30));

        var provider = services.BuildServiceProvider();

        provider.GetRequiredKeyedService<IBook<string, int>>("books").Should().NotBeNull();
        provider.GetRequiredKeyedService<IBookLoader<string, int>>("books").Should().NotBeNull();

        var targets = provider.GetServices<IBookRefreshTarget>().ToList();
        targets.Should().ContainSingle();
        targets[0].Name.Should().Be("books");
        targets[0].RefreshInterval.Should().Be(TimeSpan.FromSeconds(30));
        provider.GetServices<IHostedService>()
            .Should()
            .Contain(service => service is BooksOrchestratorHostedService);
    }

    [Fact]
    public void AddBook_BindsOptionsFromConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Books:books:RefreshInterval"] = "00:01:00",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBook<string, int, WidgetBookLoader>(configuration, "books");

        var provider = services.BuildServiceProvider();
        var target = provider.GetServices<IBookRefreshTarget>().Single();

        target.RefreshInterval.Should().Be(TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task AddBook_EndToEndRefresh_PopulatesBook()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBook<string, int, WidgetBookLoader>("books", _ => { });
        services.AddSingleton<TrackingRefreshHandler>();
        services.AddBookRefreshHandler(
            "books",
            sp => sp.GetRequiredService<TrackingRefreshHandler>());

        var provider = services.BuildServiceProvider();
        var target = provider.GetServices<IBookRefreshTarget>().Single();
        var book = provider.GetRequiredKeyedService<IBook<string, int>>("books");
        var handler = provider.GetRequiredService<TrackingRefreshHandler>();

        await target.RefreshAsync(CancellationToken.None);

        book.TryGet("widget", out var value).Should().BeTrue();
        value.Should().Be(3);
        handler.CallCount.Should().Be(1);
        handler.LastBookName.Should().Be("books");
    }

    [Fact]
    public void AddBookRefreshHandler_RegistersHandlerType()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBook<string, int, WidgetBookLoader>("books", _ => { });
        services.AddBookRefreshHandler<string, int, TrackingRefreshHandler>("books");

        var provider = services.BuildServiceProvider();
        var handlers = provider.GetKeyedServices<IBookRefreshHandler<string, int>>("books").ToList();

        handlers.Should().ContainSingle();
        handlers[0].Should().BeOfType<TrackingRefreshHandler>();
    }
}
