using SimpleModule.Core.Settings;

namespace SimpleModule.Settings.Contracts;

public class Setting
{
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public SettingScope Scope { get; set; }
    public string? UserId { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
