using System.Text.Json;
using SimpleModule.Core.Settings;
using SimpleModule.Settings.Contracts;

namespace SimpleModule.Branding.Tests;

/// <summary>
/// Minimal dictionary-backed <see cref="ISettingsContracts"/> for unit-testing
/// <c>BrandingService</c> without the full Settings infrastructure. Only the members
/// the service touches are implemented; the rest throw.
/// </summary>
public sealed class FakeSettings : ISettingsContracts
{
    private readonly Dictionary<string, JsonElement> _app = [];

    public Task<T?> GetSettingAsync<T>(string key, SettingScope scope, string? userId = null) =>
        Task.FromResult(_app.TryGetValue(key, out var v) ? v.Deserialize<T>() : default);

    public Task<string?> GetSettingAsync(string key, SettingScope scope, string? userId = null) =>
        Task.FromResult(_app.TryGetValue(key, out var v) ? v.GetString() : null);

    public Task SetSettingAsync(
        string key,
        JsonElement value,
        SettingScope scope,
        string? userId = null
    )
    {
        _app[key] = value;
        return Task.CompletedTask;
    }

    public Task SetManyAsync(IReadOnlyList<BulkSettingUpdate> updates)
    {
        foreach (var u in updates)
            _app[u.Key] = u.Value;
        return Task.CompletedTask;
    }

    public Task<string?> ResolveUserSettingAsync(string key, string userId) =>
        throw new NotSupportedException();

    public Task<JsonElement?> ResolveUserSettingElementAsync(string key, string userId) =>
        throw new NotSupportedException();

    public Task DeleteSettingAsync(string key, SettingScope scope, string? userId = null) =>
        throw new NotSupportedException();

    public Task ResetToDefaultAsync(string key, SettingScope scope, string? userId = null) =>
        throw new NotSupportedException();

    public Task<IEnumerable<SettingValueDto>> GetSettingValuesAsync(
        SettingsFilter? filter = null,
        int skip = 0,
        int take = 30
    ) => throw new NotSupportedException();

    public Task<SettingValueDto?> GetSettingValueAsync(
        string key,
        SettingScope scope,
        string? userId = null
    ) => throw new NotSupportedException();
}
