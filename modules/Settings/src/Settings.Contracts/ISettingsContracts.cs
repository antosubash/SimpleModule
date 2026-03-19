using SimpleModule.Core.Settings;

namespace SimpleModule.Settings.Contracts;

public interface ISettingsContracts
{
    Task<string?> GetSettingAsync(string key, SettingScope scope, string? userId = null);
    Task<T?> GetSettingAsync<T>(string key, SettingScope scope, string? userId = null);
    Task<string?> ResolveUserSettingAsync(string key, string userId);
    Task SetSettingAsync(string key, string value, SettingScope scope, string? userId = null);
    Task DeleteSettingAsync(string key, SettingScope scope, string? userId = null);
    Task<IEnumerable<Setting>> GetSettingsAsync(SettingsFilter? filter = null);
}
