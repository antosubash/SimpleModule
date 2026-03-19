namespace SimpleModule.Core.Settings;

public sealed class SettingsBuilder : ISettingsBuilder
{
    private readonly List<SettingDefinition> _definitions = [];

    public ISettingsBuilder Add(SettingDefinition definition)
    {
        _definitions.Add(definition);
        return this;
    }

    public List<SettingDefinition> ToList() => [.. _definitions];
}
