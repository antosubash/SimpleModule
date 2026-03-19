using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SimpleModule.Core.Settings;
using SimpleModule.Settings.Contracts;
using SimpleModule.Settings.Entities;

namespace SimpleModule.Settings;

public partial class SettingsService(
    SettingsDbContext db,
    ISettingsDefinitionRegistry definitions,
    ILogger<SettingsService> logger
) : ISettingsContracts
{
    public async Task<string?> GetSettingAsync(
        string key,
        SettingScope scope,
        string? userId = null
    )
    {
        var entity = await db.Settings.FirstOrDefaultAsync(s =>
            s.Key == key
            && s.Scope == scope
            && (scope == SettingScope.User ? s.UserId == userId : s.UserId == null)
        );
        return entity?.Value;
    }

    public async Task<T?> GetSettingAsync<T>(
        string key,
        SettingScope scope,
        string? userId = null
    )
    {
        var value = await GetSettingAsync(key, scope, userId);
        if (value is null)
            return default;

        try
        {
            return JsonSerializer.Deserialize<T>(value);
        }
        catch (JsonException ex)
        {
            LogDeserializationError(key, typeof(T).Name, ex.Message);
            return default;
        }
    }

    public async Task<string?> ResolveUserSettingAsync(string key, string userId)
    {
        var userValue = await GetSettingAsync(key, SettingScope.User, userId);
        if (userValue is not null)
            return userValue;

        var appValue = await GetSettingAsync(key, SettingScope.Application);
        if (appValue is not null)
            return appValue;

        var definition = definitions.GetDefinition(key);
        return definition?.DefaultValue;
    }

    public async Task SetSettingAsync(
        string key,
        string value,
        SettingScope scope,
        string? userId = null
    )
    {
        var existing = await db.Settings.FirstOrDefaultAsync(s =>
            s.Key == key
            && s.Scope == scope
            && (scope == SettingScope.User ? s.UserId == userId : s.UserId == null)
        );

        if (existing is not null)
        {
            existing.Value = value;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
        }
        else
        {
            db.Settings.Add(
                new SettingEntity
                {
                    Key = key,
                    Value = value,
                    Scope = scope,
                    UserId = scope == SettingScope.User ? userId : null,
                    UpdatedAt = DateTimeOffset.UtcNow,
                }
            );
        }

        await db.SaveChangesAsync();
        LogSettingUpdated(key, scope);
    }

    public async Task DeleteSettingAsync(string key, SettingScope scope, string? userId = null)
    {
        var entity = await db.Settings.FirstOrDefaultAsync(s =>
            s.Key == key
            && s.Scope == scope
            && (scope == SettingScope.User ? s.UserId == userId : s.UserId == null)
        );

        if (entity is not null)
        {
            db.Settings.Remove(entity);
            await db.SaveChangesAsync();
            LogSettingDeleted(key, scope);
        }
    }

    public async Task<IEnumerable<Setting>> GetSettingsAsync(SettingsFilter? filter = null)
    {
        var query = db.Settings.AsQueryable();

        if (filter?.Scope is not null)
            query = query.Where(s => s.Scope == filter.Scope.Value);

        if (!string.IsNullOrEmpty(filter?.Group))
        {
            var keysInGroup = definitions
                .GetDefinitions()
                .Where(d => d.Group == filter.Group)
                .Select(d => d.Key)
                .ToList();
            query = query.Where(s => keysInGroup.Contains(s.Key));
        }

        var entities = await query.ToListAsync();
        return entities.Select(e => new Setting
        {
            Key = e.Key,
            Value = e.Value,
            Scope = e.Scope,
            UserId = e.UserId,
            UpdatedAt = e.UpdatedAt,
        });
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Setting {Key} updated in scope {Scope}")]
    private partial void LogSettingUpdated(string key, SettingScope scope);

    [LoggerMessage(Level = LogLevel.Information, Message = "Setting {Key} deleted from scope {Scope}")]
    private partial void LogSettingDeleted(string key, SettingScope scope);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to deserialize setting {Key} to type {Type}: {Error}"
    )]
    private partial void LogDeserializationError(string key, string type, string error);
}
