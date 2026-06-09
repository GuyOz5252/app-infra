using ApplicationInfra.Books.Abstract;
using ApplicationInfra.Books.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ApplicationInfra.Books.Tests;

public sealed class BookRefreshTargetTests
{
    [Fact]
    public async Task RefreshAsync_LoadsDataIntoBook()
    {
        var loader = new FakeBookLoader(new Dictionary<string, int> { ["item"] = 7 });
        var book = new Book<string, int>();
        var target = CreateTarget(loader, book);

        await target.RefreshAsync(CancellationToken.None);

        loader.LoadCallCount.Should().Be(1);
        book.TryGet("item", out var value).Should().BeTrue();
        value.Should().Be(7);
    }

    [Fact]
    public async Task RefreshAsync_InvokesAllRefreshHandlers()
    {
        var handler1 = new TrackingRefreshHandler();
        var handler2 = new TrackingRefreshHandler();
        var data = new Dictionary<string, int> { ["x"] = 1 };
        var loader = new FakeBookLoader(data);
        var target = CreateTarget(loader, handlers: [handler1, handler2]);

        await target.RefreshAsync(CancellationToken.None);

        handler1.CallCount.Should().Be(1);
        handler1.LastBookName.Should().Be("test-book");
        handler1.LastData.Should().BeEquivalentTo(data);
        handler2.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task RefreshAsync_RetainsPreviousData_WhenLoaderFails()
    {
        var book = new Book<string, int>();
        book.Refresh(new Dictionary<string, int> { ["old"] = 99 });
        var loader = new FakeBookLoader(_ => throw new InvalidOperationException("load failed"));
        var target = CreateTarget(loader, book);

        var act = () => target.RefreshAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
        book.TryGet("old", out var value).Should().BeTrue();
        value.Should().Be(99);
    }

    [Fact]
    public void Constructor_SetsNameAndRefreshInterval()
    {
        var target = CreateTarget(
            new FakeBookLoader(new Dictionary<string, int>()),
            refreshInterval: TimeSpan.FromSeconds(45),
            name: "products");

        target.Name.Should().Be("products");
        target.RefreshInterval.Should().Be(TimeSpan.FromSeconds(45));
    }

    private static BookRefreshTarget<string, int> CreateTarget(
        FakeBookLoader loader,
        Book<string, int>? book = null,
        IEnumerable<IBookRefreshHandler<string, int>>? handlers = null,
        TimeSpan? refreshInterval = null,
        string name = "test-book")
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddKeyedScoped<IBookLoader<string, int>>(name, (_, _) => loader);
        var provider = services.BuildServiceProvider();

        return new BookRefreshTarget<string, int>(
            provider.GetRequiredService<ILoggerFactory>(),
            provider.GetRequiredService<IServiceScopeFactory>(),
            book ?? new Book<string, int>(),
            handlers ?? [],
            refreshInterval ?? TimeSpan.FromMinutes(1),
            name);
    }
}
