using System.Reflection;

namespace SimpleModule.Core.FeatureFlags;

public sealed class FeatureFlagRegistryBuilder
{
    private readonly HashSet<string> _allNames = [];
    private readonly Dictionary<string, FeatureFlagDefinition> _definitions = new();

    public void AddFeatures<T>()
        where T : class
    {
        var fields = typeof(T)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string));

        foreach (var field in fields)
        {
            var value = (string)field.GetRawConstantValue()!;
            _allNames.Add(value);
        }
    }

    public void AddDefinition(FeatureFlagDefinition definition)
    {
        _allNames.Add(definition.Name);
        _definitions[definition.Name] = definition;
    }

    public void AddDefinitions(List<FeatureFlagDefinition> definitions)
    {
        foreach (var definition in definitions)
        {
            AddDefinition(definition);
        }
    }

    public FeatureFlagRegistry Build()
    {
        // For names discovered via IModuleFeatures but without a definition,
        // create a default definition (disabled by default).
        var allDefinitions = new List<FeatureFlagDefinition>();
        foreach (var name in _allNames)
        {
            if (_definitions.TryGetValue(name, out var def))
            {
                allDefinitions.Add(def);
            }
            else
            {
                allDefinitions.Add(new FeatureFlagDefinition { Name = name });
            }
        }

        return new FeatureFlagRegistry(_allNames, allDefinitions);
    }
}
