namespace SimpleModule.Core.Caching;

/// <summary>
/// Helpers for composing consistent cache keys.
/// </summary>
public static class CacheKey
{
    /// <summary>
    /// Joins the supplied parts with <c>:</c>, skipping null or empty segments.
    /// </summary>
    /// <example>
    /// <code>CacheKey.Compose("settings", scope.ToString(), userId, key);</code>
    /// </example>
    public static string Compose(params string?[] parts)
    {
        ArgumentNullException.ThrowIfNull(parts);
        return string.Join(':', parts.Where(p => !string.IsNullOrEmpty(p)));
    }
}
