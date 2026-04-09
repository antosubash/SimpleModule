using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace SimpleModule.Core.Caching;

/// <summary>
/// In-process <see cref="ICacheStore"/> implementation backed by
/// <see cref="IMemoryCache"/>. Adds two capabilities on top of the raw memory cache:
/// stampede-safe <see cref="GetOrCreateAsync{T}"/> via per-key locking, and
/// <see cref="RemoveByPrefixAsync"/> via a tracked key set.
/// </summary>
public sealed class MemoryCacheStore : ICacheStore, IDisposable
{
    private readonly IMemoryCache _cache;
    private readonly ConcurrentDictionary<string, byte> _trackedKeys = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _keyLocks = new(
        StringComparer.Ordinal
    );

    public MemoryCacheStore(IMemoryCache cache)
    {
        ArgumentNullException.ThrowIfNull(cache);
        _cache = cache;
    }

    public ValueTask<CacheResult<T>> TryGetAsync<T>(
        string key,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        cancellationToken.ThrowIfCancellationRequested();

        if (_cache.TryGetValue(key, out var raw))
        {
            return ValueTask.FromResult(CacheResult.Hit((T?)raw));
        }

        return ValueTask.FromResult(CacheResult.Miss<T>());
    }

    public ValueTask SetAsync<T>(
        string key,
        T? value,
        CacheEntryOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        cancellationToken.ThrowIfCancellationRequested();

        SetCore(key, value, options);
        return ValueTask.CompletedTask;
    }

    public async ValueTask<T?> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, ValueTask<T?>> factory,
        CacheEntryOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(factory);

        if (_cache.TryGetValue(key, out var existing))
        {
            return (T?)existing;
        }

        var gate = _keyLocks.GetOrAdd(key, static _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_cache.TryGetValue(key, out existing))
            {
                return (T?)existing;
            }

            var value = await factory(cancellationToken).ConfigureAwait(false);
            SetCore(key, value, options);
            return value;
        }
        finally
        {
            gate.Release();
        }
    }

    public ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        cancellationToken.ThrowIfCancellationRequested();

        _cache.Remove(key);
        _trackedKeys.TryRemove(key, out _);
        return ValueTask.CompletedTask;
    }

    public ValueTask RemoveByPrefixAsync(
        string prefix,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(prefix);
        cancellationToken.ThrowIfCancellationRequested();

        foreach (var key in _trackedKeys.Keys)
        {
            if (key.StartsWith(prefix, StringComparison.Ordinal))
            {
                _cache.Remove(key);
                _trackedKeys.TryRemove(key, out _);
            }
        }

        return ValueTask.CompletedTask;
    }

    private void SetCore<T>(string key, T? value, CacheEntryOptions? options)
    {
        using var entry = _cache.CreateEntry(key);
        entry.Value = value;

        if (options is not null)
        {
            if (options.AbsoluteExpirationRelativeToNow is { } relative)
            {
                entry.AbsoluteExpirationRelativeToNow = relative;
            }

            if (options.AbsoluteExpiration is { } absolute)
            {
                entry.AbsoluteExpiration = absolute;
            }

            if (options.SlidingExpiration is { } sliding)
            {
                entry.SlidingExpiration = sliding;
            }

            if (options.Size is { } size)
            {
                entry.Size = size;
            }
        }

        // Track the key so RemoveByPrefixAsync can find it. The eviction callback below
        // also removes from the tracking set when the entry naturally expires or is evicted
        // by memory pressure, so the set stays bounded.
        _trackedKeys[key] = 0;
        entry.RegisterPostEvictionCallback(
            static (evictedKey, _, _, state) =>
            {
                if (state is MemoryCacheStore self && evictedKey is string s)
                {
                    self._trackedKeys.TryRemove(s, out _);
                }
            },
            this
        );
    }

    public void Dispose()
    {
        foreach (var gate in _keyLocks.Values)
        {
            gate.Dispose();
        }
        _keyLocks.Clear();
    }
}
