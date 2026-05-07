namespace ApplicationInfra.Books.Options;

public sealed class BookOptions
{
    public TimeSpan RefreshInterval { get; set; } = TimeSpan.FromMinutes(10);
}
