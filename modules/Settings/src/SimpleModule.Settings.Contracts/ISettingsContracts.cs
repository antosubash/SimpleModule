using System.Text.Json;
using SimpleModule.Core.Settings;

namespace SimpleModule.Settings.Contracts;

public interface ISettingsContracts
{
    Task<string?> GetSettingAsync(string key, SettingScope scope, string? userId = null);
    Task<T?> GetSettingAsync<T>(string key, SettingScope scope, string? userId = null);
    Task<string?> ResolveUserSettingAsync(string key, string userId);
    Task<JsonElement?> ResolveUserSettingElementAsync(string key, string userId);
    Task SetSettingAsync(string key, JsonElement value, SettingScope scope, string? userId = null);
    Task SetManyAsync(IReadOnlyList<BulkSettingUpdate> updates);
    Task DeleteSettingAsync(string key, SettingScope scope, string? userId = null);
    Task ResetToDefaultAsync(string key, SettingScope scope, string? userId = null);
    Task<IEnumerable<SettingValueDto>> GetSettingValuesAsync(SettingsFilter? filter = null);
    Task<SettingValueDto?> GetSettingValueAsync(
        string key,
        SettingScope scope,
        string? userId = null
    );
}
