namespace SimpleModule.Core.Settings;

public sealed class SettingsDefinitionRegistry(List<SettingDefinition> definitions)
    : ISettingsDefinitionRegistry
{
    private readonly Dictionary<string, SettingDefinition> _byKey =
        definitions.ToDictionary(d => d.Key);

    public IReadOnlyList<SettingDefinition> GetDefinitions(SettingScope? scope = null)
    {
        if (scope is null)
            return definitions.AsReadOnly();

        return definitions.Where(d => d.Scope == scope.Value).ToList().AsReadOnly();
    }

    public SettingDefinition? GetDefinition(string key) =>
        _byKey.GetValueOrDefault(key);
}
