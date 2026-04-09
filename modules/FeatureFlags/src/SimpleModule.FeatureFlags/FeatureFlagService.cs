using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SimpleModule.Core.Caching;
using SimpleModule.Core.Entities;
using SimpleModule.Core.FeatureFlags;
using SimpleModule.FeatureFlags.Contracts;
using SimpleModule.FeatureFlags.Entities;

namespace SimpleModule.FeatureFlags;

public sealed partial class FeatureFlagService(
    FeatureFlagsDbContext db,
    IFeatureFlagRegistry registry,
    ICacheStore cache,
    ILogger<FeatureFlagService> logger,
    IServiceProvider serviceProvider
) : IFeatureFlagContracts, IFeatureFlagService
{
    private readonly Lazy<ITenantContext?> _tenantContext = new(() =>
        serviceProvider.GetService<ITenantContext>()
    );
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);
    private const string AllFlagDataCacheKey = "ff:all-data";

    private sealed record FlagData(
        bool IsEnabled,
        Dictionary<string, bool> UserOverrides,
        Dictionary<string, bool> RoleOverrides,
        Dictionary<string, bool> TenantOverrides
    );

    public async Task<bool> IsEnabledAsync(
        string flagName,
        string? userId = null,
        IEnumerable<string>? roles = null
    )
    {
        var data = await GetFlagDataAsync(flagName);
        return ResolveFlagState(data, userId, roles);
    }

    public async Task<Dictionary<string, bool>> GetAllEnabledAsync(
        string? userId = null,
        IEnumerable<string>? roles = null
    )
    {
        var allData = await GetAllFlagDataAsync();
        var rolesList = roles as IReadOnlyList<string> ?? roles?.ToList();

        var result = new Dictionary<string, bool>(allData.Count);
        foreach (var (name, data) in allData)
        {
            result[name] = ResolveFlagState(data, userId, rolesList);
        }

        return result;
    }

    public async Task<IEnumerable<FeatureFlag>> GetAllFlagsAsync()
    {
        var definitions = registry.GetAllDefinitions();
        var dbFlags = await db.FeatureFlags.AsNoTracking().ToListAsync();
        var dbMap = dbFlags.ToDictionary(f => f.Name);

        var result = new List<FeatureFlag>();
        var addedNames = new HashSet<string>(definitions.Count, StringComparer.Ordinal);

        foreach (var def in definitions)
        {
            addedNames.Add(def.Name);
            dbMap.TryGetValue(def.Name, out var entity);
            result.Add(ToDto(def, entity));
        }

        // Include deprecated flags (in DB but not in registry)
        foreach (var entity in dbFlags)
        {
            if (entity.IsDeprecated && !addedNames.Contains(entity.Name))
            {
                result.Add(
                    new FeatureFlag
                    {
                        Name = entity.Name,
                        IsEnabled = entity.IsEnabled,
                        IsDeprecated = true,
                        UpdatedAt = entity.UpdatedAt,
                    }
                );
            }
        }

        return result;
    }

    public async Task<FeatureFlag?> GetFlagAsync(string flagName)
    {
        var def = registry.GetDefinition(flagName);
        var entity = await db
            .FeatureFlags.AsNoTracking()
            .FirstOrDefaultAsync(f => f.Name == flagName);

        if (def is null && entity is null)
        {
            return null;
        }

        return ToDto(def, entity);
    }

    public async Task<FeatureFlag> UpdateFlagAsync(
        string flagName,
        UpdateFeatureFlagRequest request
    )
    {
        var entity = await db.FeatureFlags.FirstOrDefaultAsync(f => f.Name == flagName);
        if (entity is null)
        {
            entity = new FeatureFlagEntity { Name = flagName, IsEnabled = request.IsEnabled };
            db.FeatureFlags.Add(entity);
        }
        else
        {
            entity.IsEnabled = request.IsEnabled;
        }

        await db.SaveChangesAsync();
        await InvalidateCacheAsync(flagName);

        LogFlagToggled(logger, flagName, request.IsEnabled);

        var def = registry.GetDefinition(flagName);
        return ToDto(def, entity);
    }

    public async Task<IEnumerable<FeatureFlagOverride>> GetOverridesAsync(string flagName)
    {
        return await db
            .FeatureFlagOverrides.AsNoTracking()
            .Where(o => o.FlagName == flagName)
            .Select(o => new FeatureFlagOverride
            {
                Id = o.Id,
                FlagName = o.FlagName,
                OverrideType = o.OverrideType,
                OverrideValue = o.OverrideValue,
                IsEnabled = o.IsEnabled,
            })
            .ToListAsync();
    }

    public async Task<FeatureFlagOverride> SetOverrideAsync(
        string flagName,
        SetOverrideRequest request
    )
    {
        var existing = await db.FeatureFlagOverrides.FirstOrDefaultAsync(o =>
            o.FlagName == flagName
            && o.OverrideType == request.OverrideType
            && o.OverrideValue == request.OverrideValue
        );

        if (existing is not null)
        {
            existing.IsEnabled = request.IsEnabled;
        }
        else
        {
            existing = new FeatureFlagOverrideEntity
            {
                FlagName = flagName,
                OverrideType = request.OverrideType,
                OverrideValue = request.OverrideValue,
                IsEnabled = request.IsEnabled,
            };
            db.FeatureFlagOverrides.Add(existing);
        }

        await db.SaveChangesAsync();
        await InvalidateCacheAsync(flagName);

        return new FeatureFlagOverride
        {
            Id = existing.Id,
            FlagName = existing.FlagName,
            OverrideType = existing.OverrideType,
            OverrideValue = existing.OverrideValue,
            IsEnabled = existing.IsEnabled,
        };
    }

    public async Task DeleteOverrideAsync(int overrideId)
    {
        var entity = await db.FeatureFlagOverrides.FindAsync(overrideId);
        if (entity is not null)
        {
            db.FeatureFlagOverrides.Remove(entity);
            await db.SaveChangesAsync();
            await InvalidateCacheAsync(entity.FlagName);
        }
    }

    private async Task<Dictionary<string, FlagData>> GetAllFlagDataAsync()
    {
        var result = await cache.GetOrCreateAsync<Dictionary<string, FlagData>>(
            AllFlagDataCacheKey,
            async ct =>
            {
                var definitions = registry.GetAllDefinitions();
                var flagNames = definitions.Select(d => d.Name).ToList();

                var dbFlags = await db
                    .FeatureFlags.AsNoTracking()
                    .Where(f => flagNames.Contains(f.Name))
                    .ToDictionaryAsync(f => f.Name, ct);

                var dbOverrides = await db
                    .FeatureFlagOverrides.AsNoTracking()
                    .Where(o => flagNames.Contains(o.FlagName))
                    .ToListAsync(ct);

                var overridesByFlag = dbOverrides
                    .GroupBy(o => o.FlagName)
                    .ToDictionary(g => g.Key, g => g.ToList());

                var allData = new Dictionary<string, FlagData>(
                    flagNames.Count,
                    StringComparer.Ordinal
                );
                foreach (var def in definitions)
                {
                    var isEnabled = dbFlags.TryGetValue(def.Name, out var entity)
                        ? entity.IsEnabled
                        : def.DefaultEnabled;

                    var flagOverrides = overridesByFlag.GetValueOrDefault(def.Name, []);
                    var data = BuildFlagData(isEnabled, flagOverrides);

                    allData[def.Name] = data;
                    await cache.SetAsync(
                        $"ff:data:{def.Name}",
                        data,
                        CacheEntryOptions.Expires(CacheDuration),
                        ct
                    );
                }

                return allData;
            },
            CacheEntryOptions.Expires(CacheDuration)
        );
        return result ?? [];
    }

    private async Task<FlagData> GetFlagDataAsync(string flagName)
    {
        var cacheKey = $"ff:data:{flagName}";
        var result = await cache.GetOrCreateAsync<FlagData>(
            cacheKey,
            async ct =>
            {
                var flag = await db
                    .FeatureFlags.AsNoTracking()
                    .FirstOrDefaultAsync(f => f.Name == flagName, ct);

                var isEnabled =
                    flag?.IsEnabled ?? registry.GetDefinition(flagName)?.DefaultEnabled ?? false;

                var overrides = await db
                    .FeatureFlagOverrides.AsNoTracking()
                    .Where(o => o.FlagName == flagName)
                    .ToListAsync(ct);

                return BuildFlagData(isEnabled, overrides);
            },
            CacheEntryOptions.Expires(CacheDuration)
        );
        return result ?? BuildFlagData(false, []);
    }

    private static FlagData BuildFlagData(bool isEnabled, List<FeatureFlagOverrideEntity> overrides)
    {
        var userOverrides = new Dictionary<string, bool>(StringComparer.Ordinal);
        var roleOverrides = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        var tenantOverrides = new Dictionary<string, bool>(StringComparer.Ordinal);

        foreach (var o in overrides)
        {
            switch (o.OverrideType)
            {
                case OverrideType.User:
                    userOverrides[o.OverrideValue] = o.IsEnabled;
                    break;
                case OverrideType.Role:
                    roleOverrides[o.OverrideValue] = o.IsEnabled;
                    break;
                case OverrideType.Tenant:
                    tenantOverrides[o.OverrideValue] = o.IsEnabled;
                    break;
            }
        }

        return new FlagData(isEnabled, userOverrides, roleOverrides, tenantOverrides);
    }

    private bool ResolveFlagState(FlagData data, string? userId, IEnumerable<string>? roles)
    {
        if (userId is not null && data.UserOverrides.TryGetValue(userId, out var userEnabled))
        {
            return userEnabled;
        }

        if (roles is not null)
        {
            foreach (var role in roles)
            {
                if (data.RoleOverrides.TryGetValue(role, out var roleEnabled))
                {
                    return roleEnabled;
                }
            }
        }

        var currentTenantId = _tenantContext.Value?.TenantId;
        if (
            currentTenantId is not null
            && data.TenantOverrides.TryGetValue(currentTenantId, out var tenantEnabled)
        )
        {
            return tenantEnabled;
        }

        return data.IsEnabled;
    }

    private static FeatureFlag ToDto(FeatureFlagDefinition? def, FeatureFlagEntity? entity)
    {
        return new FeatureFlag
        {
            Name = def?.Name ?? entity?.Name ?? string.Empty,
            Description = def?.Description ?? string.Empty,
            IsEnabled = entity?.IsEnabled ?? def?.DefaultEnabled ?? false,
            DefaultEnabled = def?.DefaultEnabled ?? false,
            IsDeprecated = entity?.IsDeprecated ?? false,
            UpdatedAt = entity?.UpdatedAt ?? default,
        };
    }

    private async ValueTask InvalidateCacheAsync(string flagName)
    {
        await cache.RemoveAsync($"ff:data:{flagName}");
        await cache.RemoveAsync(AllFlagDataCacheKey);
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Feature flag '{FlagName}' toggled to {IsEnabled}"
    )]
    private static partial void LogFlagToggled(ILogger logger, string flagName, bool isEnabled);
}
