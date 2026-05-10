namespace ApplicationInfra.Books.Abstract;

internal interface IBookRefreshTarget
{
    string Name { get; }
    TimeSpan RefreshInterval { get; }
    Task RefreshAsync(CancellationToken cancellationToken);
}
