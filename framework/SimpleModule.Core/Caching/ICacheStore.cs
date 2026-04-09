namespace SimpleModule.Core.Caching;

/// <summary>
/// Unified caching abstraction used across SimpleModule modules.
/// </summary>
/// <remarks>
/// The default registration is an in-process <c>MemoryCacheStore</c> backed by
/// <see cref="Microsoft.Extensions.Caching.Memory.IMemoryCache"/>. The interface is intentionally
/// async-first so that distributed implementations (Redis, etc.) can be plugged in without
/// changing call sites.
/// </remarks>
public interface ICacheStore
{
    /// <summary>
    /// Looks up an entry. Returns a <see cref="CacheResult{T}"/> that distinguishes a miss from a
    /// hit containing a <see langword="null"/> value (negative caching).
    /// </summary>
    ValueTask<CacheResult<T>> TryGetAsync<T>(
        string key,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Writes an entry, replacing any existing value for <paramref name="key"/>.
    /// </summary>
    ValueTask SetAsync<T>(
        string key,
        T? value,
        CacheEntryOptions? options = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Returns the cached value for <paramref name="key"/>, invoking <paramref name="factory"/>
    /// to populate the cache on a miss. Implementations must guard against cache stampedes —
    /// concurrent callers for the same key see <paramref name="factory"/> invoked at most once.
    /// </summary>
    ValueTask<T?> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, ValueTask<T?>> factory,
        CacheEntryOptions? options = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Removes a single entry. No-op if the key is absent.
    /// </summary>
    ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes every entry whose key starts with <paramref name="prefix"/>. Useful for
    /// invalidating a logical group (e.g., all entries for a user, tenant, or module).
    /// </summary>
    ValueTask RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default);
}
