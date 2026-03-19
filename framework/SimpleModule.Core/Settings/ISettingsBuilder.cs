namespace SimpleModule.Core.Settings;

public interface ISettingsBuilder
{
    ISettingsBuilder Add(SettingDefinition definition);
}
