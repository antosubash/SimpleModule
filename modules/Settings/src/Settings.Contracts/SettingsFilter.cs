using SimpleModule.Core.Settings;

namespace SimpleModule.Settings.Contracts;

public class SettingsFilter
{
    public SettingScope? Scope { get; set; }
    public string? Group { get; set; }
}
