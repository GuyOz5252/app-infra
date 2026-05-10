namespace ApplicationInfra.Books.Abstract;

/// <summary>
/// Handles notifications fired after a book is successfully refreshed.
/// </summary>
/// <typeparam name="TKey">The key type of the book entries.</typeparam>
/// <typeparam name="TValue">The value type of the book entries.</typeparam>
public interface IBookRefreshHandler<TKey, TValue>
    where TKey : notnull
{
    /// <summary>
    /// Called after a successful refresh of the book identified by <paramref name="bookName"/>.
    /// </summary>
    /// <param name="bookName">The name of the book that was refreshed.</param>
    /// <param name="data">The full, newly loaded data snapshot.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task OnRefreshedAsync(
        string bookName,
        IReadOnlyDictionary<TKey, TValue> data,
        CancellationToken cancellationToken);
}
