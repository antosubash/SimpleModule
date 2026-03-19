namespace SimpleModule.Core.Inertia;

public sealed class InertiaSharedData
{
    private readonly Dictionary<string, object?> _data = [];

    public void Set(string key, object? value) => _data[key] = value;

    public IReadOnlyDictionary<string, object?> All => _data;
}
