using System.Text.Json;
using SimpleModule.Core;
using SimpleModule.Core.Settings;

namespace SimpleModule.Settings.Contracts;

[Dto]
public class UpdateSettingRequest
{
    public string Key { get; set; } = string.Empty;
    public SettingScope Scope { get; set; }
    public JsonElement Value { get; set; }
}
