using System.Text.Json;
using SimpleModule.Core;
using SimpleModule.Core.Settings;

namespace SimpleModule.Settings.Contracts;

[Dto]
public class SettingValueDto
{
    public string Key { get; set; } = "";
    public SettingScope Scope { get; set; }
    public JsonElement? Value { get; set; }
    public bool IsOverridden { get; set; }
    public string? UserId { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

[Dto]
public class UserSettingValueDto
{
    public string Key { get; set; } = "";
    public JsonElement? Value { get; set; }
    public JsonElement? ResolvedValue { get; set; }
    public bool IsOverridden { get; set; }
}
