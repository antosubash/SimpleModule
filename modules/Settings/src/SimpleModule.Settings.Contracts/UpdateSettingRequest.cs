using SimpleModule.Core.Settings;

namespace SimpleModule.Settings.Contracts;

public class UpdateSettingRequest
{
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public SettingScope Scope { get; set; }
}
