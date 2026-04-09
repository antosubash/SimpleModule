namespace SimpleModule.Core.Caching;

/// <summary>
/// Convenience extensions over <see cref="ICacheStore"/>.
/// </summary>
public static class CacheStoreExtensions
{
    /// <summary>
    /// Returns a view over the store where every key is automatically prefixed with
    /// <paramref name="prefix"/> (joined with <c>:</c>). Useful for module- or tenant-scoped
    /// cache namespacing without forcing every call site to remember the prefix.
    /// </summary>
    public static ICacheStore WithPrefix(this ICacheStore store, string prefix)
    {
        ArgumentNullException.ThrowIfNull(store);
        return new PrefixedCacheStore(store, prefix);
    }

    /// <summary>
    /// Synchronous-style helper for the common pattern <c>var v = await cache.GetOrCreateAsync(...)</c>
    /// where the factory is itself synchronous.
    /// </summary>
    public static ValueTask<T?> GetOrCreateAsync<T>(
        this ICacheStore store,
        string key,
        Func<T?> factory,
        CacheEntryOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(factory);
        return store.GetOrCreateAsync(
            key,
            _ => new ValueTask<T?>(factory()),
            options,
            cancellationToken
        );
    }
}
