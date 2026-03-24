namespace SimpleModule.Core.Inertia;

/// <summary>
/// Per-request scoped service for sharing props across all Inertia responses in a single HTTP request.
/// </summary>
public sealed class InertiaSharedData
{
    private readonly Dictionary<string, object?> _data = [];

    /// <summary>
    /// Sets or overwrites a shared prop for all Inertia responses in this request.
    /// </summary>
    public void Set(string key, object? value) => _data[key] = value;

    /// <summary>
    /// Gets a shared prop by key with type safety, or returns the default value if not found.
    /// </summary>
    public T? Get<T>(string key, T? defaultValue = default) =>
        _data.TryGetValue(key, out var value) && value is T typed ? typed : defaultValue;

    /// <summary>
    /// Removes a shared prop by key. Returns true if the key existed.
    /// </summary>
    public bool Remove(string key) => _data.Remove(key);

    /// <summary>
    /// Checks if a shared prop exists by key.
    /// </summary>
    public bool Contains(string key) => _data.ContainsKey(key);

    /// <summary>
    /// All shared props (read-only). Updated via Set/Remove methods.
    /// </summary>
    public IReadOnlyDictionary<string, object?> All => _data;
}
