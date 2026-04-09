namespace SimpleModule.Core.Caching;

/// <summary>
/// Result of a cache lookup. Distinguishes a miss from a hit that contains a <see langword="null"/>
/// value (negative caching).
/// </summary>
/// <typeparam name="T">The cached value type.</typeparam>
public readonly record struct CacheResult<T>(bool Hit, T? Value);

/// <summary>
/// Non-generic helpers for constructing <see cref="CacheResult{T}"/> values.
/// </summary>
public static class CacheResult
{
    /// <summary>
    /// Creates a miss result for type <typeparamref name="T"/>.
    /// </summary>
    public static CacheResult<T> Miss<T>() => default;

    /// <summary>
    /// Creates a hit result with the supplied value (which may be <see langword="null"/>).
    /// </summary>
    public static CacheResult<T> Hit<T>(T? value) => new(true, value);
}
