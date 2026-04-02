namespace SimpleModule.Core.FeatureFlags;

public sealed class FeatureFlagRegistry : IFeatureFlagRegistry
{
    private readonly Dictionary<string, FeatureFlagDefinition> _definitions;

    public FeatureFlagRegistry(HashSet<string> allNames, List<FeatureFlagDefinition> definitions)
    {
        // allNames parameter kept for API compatibility with generated code.
        // Build() already ensures definitions contains entries for all names.
        _ = allNames;
        _definitions = definitions.ToDictionary(d => d.Name);
    }

    public FeatureFlagDefinition? GetDefinition(string name) =>
        _definitions.GetValueOrDefault(name);

    public IReadOnlyList<FeatureFlagDefinition> GetAllDefinitions() => _definitions.Values.ToList();

    public bool IsKnownFeature(string name) => _definitions.ContainsKey(name);

    public IReadOnlySet<string> GetAllFeatureNames() =>
        _definitions.Keys.ToHashSet(StringComparer.Ordinal);
}
