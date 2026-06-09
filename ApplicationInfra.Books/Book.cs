using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using ApplicationInfra.Books.Abstract;

namespace ApplicationInfra.Books;

internal sealed class Book<TKey, TValue> : IBook<TKey, TValue>
    where TKey : notnull
{
    private IReadOnlyDictionary<TKey, TValue> _data = ImmutableDictionary<TKey, TValue>.Empty;

    public event EventHandler? OnRefresh;
    
    internal void Refresh(IReadOnlyDictionary<TKey, TValue> data)
    {
        Volatile.Write(ref _data, data);
    }

    internal void RaiseOnRefresh()
    {
        OnRefresh?.Invoke(this, EventArgs.Empty);
    }
    
    public bool TryGet(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        return _data.TryGetValue(key, out value);
    }

    public IReadOnlyDictionary<TKey, TValue> GetAll()
    {
        return Volatile.Read(ref _data);
    }
}
