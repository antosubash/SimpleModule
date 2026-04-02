namespace SimpleModule.BackgroundJobs.Services;

public sealed class JobTypeRegistry
{
    private readonly Dictionary<string, Type> _registry = new(StringComparer.Ordinal);

    public void Register(Type jobType)
    {
        _registry[jobType.AssemblyQualifiedName!] = jobType;
    }

    public Type? Resolve(string typeName)
    {
        return _registry.GetValueOrDefault(typeName);
    }

    public bool IsRegistered(string typeName)
    {
        return _registry.ContainsKey(typeName);
    }

    public IReadOnlyDictionary<string, Type> All => _registry;
}
