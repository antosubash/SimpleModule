using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SimpleModule.Core.Settings;
using SimpleModule.Settings.Contracts;
using SimpleModule.Settings.Contracts.Events;
using Wolverine;
using ZiggyCreatures.Caching.Fusion;

namespace SimpleModule.Settings;

public sealed partial class SettingsService(
    SettingsDbContext db,
    ISettingsDefinitionRegistry definitions,
    IFusionCache cache,
    Lazy<IMessageBus> bus,
    IOptions<SettingsModuleOptions> moduleOptions,
    ILogger<SettingsService> logger
) : ISettingsContracts
{
    private readonly FusionCacheEntryOptions _cacheOptions = new()
    {
        Duration = moduleOptions.Value.CacheDuration,
    };

    public async Task<string?> GetSettingAsync(
        string key,
        SettingScope scope,
        string? userId = null
    )
    {
        var cacheKey = BuildCacheKey(key, scope, userId);

        return await cache.GetOrSetAsync<string?>(
            cacheKey,
            async (_, ct) =>
            {
                var entity = await db
                    .Settings.AsNoTracking()
                    .FirstOrDefaultAsync(
                        s =>
                            s.Key == key
                            && s.Scope == scope
                            && (scope == SettingScope.User ? s.UserId == userId : s.UserId == null),
                        ct
                    );
                return entity?.Value;
            },
            _cacheOptions
        );
    }

    public async Task<T?> GetSettingAsync<T>(string key, SettingScope scope, string? userId = null)
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

    public async Task<JsonElement?> ResolveUserSettingElementAsync(string key, string userId)
    {
        var raw = await ResolveUserSettingAsync(key, userId);
        if (raw is null)
            return null;

        return ParseElement(raw);
    }

    public async Task SetSettingAsync(
        string key,
        JsonElement value,
        SettingScope scope,
        string? userId = null
    )
    {
        var definition = definitions.GetDefinition(key);

        if (definition is not null)
        {
            var errors = SettingValidator.Validate(definition, value);
            if (errors.Count > 0)
                throw new SettingValidationException(key, errors);
        }

        var storageValue = value.GetRawText();

        var existing = await db.Settings.FirstOrDefaultAsync(s =>
            s.Key == key
            && s.Scope == scope
            && (scope == SettingScope.User ? s.UserId == userId : s.UserId == null)
        );

        var oldValue = existing?.Value;

        if (existing is not null)
        {
            existing.Value = storageValue;
        }
        else
        {
            db.Settings.Add(
                new SettingEntity
                {
                    Key = key,
                    Value = storageValue,
                    Scope = scope,
                    UserId = scope == SettingScope.User ? userId : null,
                }
            );
        }

        await db.SaveChangesAsync();
        await cache.RemoveAsync(BuildCacheKey(key, scope, userId));
        LogSettingUpdated(key, scope);

        // IMessageBus is Lazy to break the SettingsService → IMessageBus → AuditingMessageBus
        // → ISettingsContracts → SettingsService cycle at construction time.
        await bus.Value.PublishAsync(new SettingChangedEvent(key, oldValue, storageValue, scope));
    }

    public async Task SetManyAsync(IReadOnlyList<BulkSettingUpdate> updates)
    {
        foreach (var update in updates)
        {
            var definition = definitions.GetDefinition(update.Key);
            if (definition is not null)
            {
                var errors = SettingValidator.Validate(definition, update.Value);
                if (errors.Count > 0)
                    throw new SettingValidationException(update.Key, errors);
            }
        }

        var events = new List<SettingChangedEvent>(updates.Count);

        foreach (var update in updates)
        {
            if (update.Scope == SettingScope.User)
                throw new InvalidOperationException(
                    "BulkUpdateSettings does not support User scope; use UpdateMySetting for user-scoped values."
                );

            var storageValue = update.Value.GetRawText();

            var existing = await db.Settings.FirstOrDefaultAsync(s =>
                s.Key == update.Key && s.Scope == update.Scope && s.UserId == null
            );

            var oldValue = existing?.Value;

            if (existing is not null)
            {
                existing.Value = storageValue;
            }
            else
            {
                db.Settings.Add(
                    new SettingEntity
                    {
                        Key = update.Key,
                        Value = storageValue,
                        Scope = update.Scope,
                        UserId = null,
                    }
                );
            }

            events.Add(new SettingChangedEvent(update.Key, oldValue, storageValue, update.Scope));
        }

        await db.SaveChangesAsync();

        foreach (var evt in events)
        {
            await cache.RemoveAsync(BuildCacheKey(evt.Key, evt.Scope, null));
            LogSettingUpdated(evt.Key, evt.Scope);
            await bus.Value.PublishAsync(evt);
        }
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
            await cache.RemoveAsync(BuildCacheKey(key, scope, userId));
            LogSettingDeleted(key, scope);

            await bus.Value.PublishAsync(new SettingDeletedEvent(key, scope));
        }
    }

    public Task ResetToDefaultAsync(string key, SettingScope scope, string? userId = null) =>
        DeleteSettingAsync(key, scope, userId);

    public async Task<IEnumerable<SettingValueDto>> GetSettingValuesAsync(
        SettingsFilter? filter = null
    )
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

        var entities = await query
            .AsNoTracking()
            .Select(e => new
            {
                e.Key,
                e.Value,
                e.Scope,
                e.UserId,
                e.UpdatedAt,
            })
            .ToListAsync();

        return entities.Select(e => new SettingValueDto
        {
            Key = e.Key,
            Scope = e.Scope,
            Value = IsSensitive(e.Key) ? null : ParseElement(e.Value),
            IsOverridden = true,
            UserId = e.UserId,
            UpdatedAt = e.UpdatedAt,
        });
    }

    public async Task<SettingValueDto?> GetSettingValueAsync(
        string key,
        SettingScope scope,
        string? userId = null
    )
    {
        var entity = await db
            .Settings.AsNoTracking()
            .FirstOrDefaultAsync(s =>
                s.Key == key
                && s.Scope == scope
                && (scope == SettingScope.User ? s.UserId == userId : s.UserId == null)
            );

        if (entity is null)
            return null;

        return new SettingValueDto
        {
            Key = entity.Key,
            Scope = entity.Scope,
            Value = IsSensitive(key) ? null : ParseElement(entity.Value),
            IsOverridden = true,
            UserId = entity.UserId,
            UpdatedAt = entity.UpdatedAt,
        };
    }

    private bool IsSensitive(string key)
    {
        // Sensitive flag will be added to SettingDefinition by Specialist 1.
        // Returning false until then keeps the build passing.
        _ = definitions.GetDefinition(key);
        return false;
    }

    private static JsonElement? ParseElement(string? raw)
    {
        if (raw is null)
            return null;

        try
        {
            return JsonSerializer.Deserialize<JsonElement>(raw);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Setting {Key} updated in scope {Scope}"
    )]
    private partial void LogSettingUpdated(string key, SettingScope scope);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Setting {Key} deleted from scope {Scope}"
    )]
    private partial void LogSettingDeleted(string key, SettingScope scope);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to deserialize setting {Key} to type {Type}: {Error}"
    )]
    private partial void LogDeserializationError(string key, string type, string error);

    private static string BuildCacheKey(string key, SettingScope scope, string? userId) =>
        string.IsNullOrEmpty(userId) ? $"setting:{scope}:{key}" : $"setting:{scope}:{userId}:{key}";
}
