namespace ApplicationInfra.Books.Abstract;

public interface IBookLoader<TKey, TValue>
    where TKey : notnull
{
    Task<IReadOnlyDictionary<TKey, TValue>> LoadAsync(CancellationToken cancellationToken);
}
