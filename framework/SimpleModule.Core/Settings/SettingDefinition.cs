namespace SimpleModule.Core.Settings;

public class SettingDefinition
{
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Group { get; set; }
    public SettingScope Scope { get; set; }
    public string? DefaultValue { get; set; }
    public SettingType Type { get; set; }
}
