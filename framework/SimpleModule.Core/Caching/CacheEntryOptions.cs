namespace SimpleModule.Core.Caching;

/// <summary>
/// Implementation-agnostic options describing how an entry should be retained in the cache.
/// </summary>
public sealed class CacheEntryOptions
{
    /// <summary>
    /// Lifetime relative to the time the entry is written. Mutually exclusive with
    /// <see cref="AbsoluteExpiration"/>.
    /// </summary>
    public TimeSpan? AbsoluteExpirationRelativeToNow { get; init; }

    /// <summary>
    /// An absolute point in time at which the entry expires. Mutually exclusive with
    /// <see cref="AbsoluteExpirationRelativeToNow"/>.
    /// </summary>
    public DateTimeOffset? AbsoluteExpiration { get; init; }

    /// <summary>
    /// Sliding expiration window. The entry is evicted if it is not accessed within this window.
    /// </summary>
    public TimeSpan? SlidingExpiration { get; init; }

    /// <summary>
    /// Optional size hint, used by stores that enforce a size limit.
    /// </summary>
    public long? Size { get; init; }

    /// <summary>
    /// Creates options that expire after the supplied duration.
    /// </summary>
    public static CacheEntryOptions Expires(TimeSpan duration) =>
        new() { AbsoluteExpirationRelativeToNow = duration };

    /// <summary>
    /// Creates options with a sliding expiration window.
    /// </summary>
    public static CacheEntryOptions Sliding(TimeSpan window) =>
        new() { SlidingExpiration = window };
}
