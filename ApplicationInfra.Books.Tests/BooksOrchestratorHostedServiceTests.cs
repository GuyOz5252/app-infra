using ApplicationInfra.Books.Tests.Support;

namespace ApplicationInfra.Books.Tests;

public sealed class BooksOrchestratorHostedServiceTests
{
    [Fact]
    public async Task StartAsync_RefreshesAllTargetsOnStartup()
    {
        var target1 = new FakeBookRefreshTarget { Name = "one" };
        var target2 = new FakeBookRefreshTarget { Name = "two" };
        using var service = new BooksOrchestratorHostedService([target1, target2]);

        await service.StartAsync(CancellationToken.None);
        await service.StopAsync(CancellationToken.None);

        target1.RefreshCount.Should().Be(1);
        target2.RefreshCount.Should().Be(1);
    }
}
