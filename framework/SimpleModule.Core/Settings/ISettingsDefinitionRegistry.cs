namespace SimpleModule.Core.Settings;

public interface ISettingsDefinitionRegistry
{
    IReadOnlyList<SettingDefinition> GetDefinitions(SettingScope? scope = null);
    SettingDefinition? GetDefinition(string key);
}
