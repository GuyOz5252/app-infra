using System.Diagnostics.CodeAnalysis;

namespace ApplicationInfra.Books.Abstract;

public interface IBook<TKey, TValue>
    where TKey : notnull
{
    event EventHandler OnRefresh;
        
    bool TryGet(TKey key, [MaybeNullWhen(false)] out TValue value);
    IReadOnlyDictionary<TKey, TValue> GetAll();
}
