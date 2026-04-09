namespace SimpleModule.Core.Caching;

/// <summary>
/// Decorator that scopes every key with a fixed prefix before forwarding to the inner store.
/// Created via <see cref="CacheStoreExtensions.WithPrefix"/>.
/// </summary>
internal sealed class PrefixedCacheStore : ICacheStore
{
    private readonly ICacheStore _inner;
    private readonly string _prefix;

    public PrefixedCacheStore(ICacheStore inner, string prefix)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentException.ThrowIfNullOrEmpty(prefix);
        _inner = inner;
        _prefix = prefix.EndsWith(':') ? prefix : prefix + ':';
    }

    private string Scope(string key) => _prefix + key;

    public ValueTask<CacheResult<T>> TryGetAsync<T>(
        string key,
        CancellationToken cancellationToken = default
    ) => _inner.TryGetAsync<T>(Scope(key), cancellationToken);

    public ValueTask SetAsync<T>(
        string key,
        T? value,
        CacheEntryOptions? options = null,
        CancellationToken cancellationToken = default
    ) => _inner.SetAsync(Scope(key), value, options, cancellationToken);

    public ValueTask<T?> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, ValueTask<T?>> factory,
        CacheEntryOptions? options = null,
        CancellationToken cancellationToken = default
    ) => _inner.GetOrCreateAsync(Scope(key), factory, options, cancellationToken);

    public ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default) =>
        _inner.RemoveAsync(Scope(key), cancellationToken);

    public ValueTask RemoveByPrefixAsync(
        string prefix,
        CancellationToken cancellationToken = default
    ) => _inner.RemoveByPrefixAsync(_prefix + prefix, cancellationToken);
}
