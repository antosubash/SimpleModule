using System.Text.Json;
using SimpleModule.Core;
using SimpleModule.Core.Settings;

namespace SimpleModule.Settings.Contracts;

[Dto]
public class BulkSettingUpdate
{
    public string Key { get; set; } = string.Empty;
    public SettingScope Scope { get; set; }
    public JsonElement Value { get; set; }
}

[Dto]
public class BulkUpdateSettingsRequest
{
    public IReadOnlyList<BulkSettingUpdate> Updates { get; set; } = [];
}
