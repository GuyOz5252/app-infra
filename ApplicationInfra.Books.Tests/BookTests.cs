namespace ApplicationInfra.Books.Tests;

public sealed class BookTests
{
    [Fact]
    public void TryGet_ReturnsFalse_WhenBookIsEmpty()
    {
        var book = new Book<string, int>();

        var found = book.TryGet("missing", out var value);

        found.Should().BeFalse();
        value.Should().Be(0);
    }

    [Fact]
    public void TryGet_ReturnsTrue_AfterRefresh()
    {
        var book = new Book<string, int>();
        book.Refresh(new Dictionary<string, int> { ["alpha"] = 42 });

        var found = book.TryGet("alpha", out var value);

        found.Should().BeTrue();
        value.Should().Be(42);
    }

    [Fact]
    public void GetAll_ReturnsEmptyDictionary_Initially()
    {
        var book = new Book<string, int>();

        book.GetAll().Should().BeEmpty();
    }

    [Fact]
    public void Refresh_ReplacesExistingData()
    {
        var book = new Book<string, int>();
        book.Refresh(new Dictionary<string, int> { ["first"] = 1 });
        book.Refresh(new Dictionary<string, int> { ["second"] = 2 });

        book.GetAll().Should().BeEquivalentTo(new Dictionary<string, int> { ["second"] = 2 });
        book.TryGet("first", out _).Should().BeFalse();
        book.TryGet("second", out var value).Should().BeTrue();
        value.Should().Be(2);
    }
}
