using Microsoft.EntityFrameworkCore;
using SimpleModule.Core.Caching;
using SimpleModule.Core.Entities;
using SimpleModule.Core.FeatureFlags;
using SimpleModule.FeatureFlags.Contracts;

namespace SimpleModule.FeatureFlags;

public sealed partial class FeatureFlagService
{
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
                        FlagDataCacheKey(def.Name),
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
        var result = await cache.GetOrCreateAsync<FlagData>(
            FlagDataCacheKey(flagName),
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
        await cache.RemoveAsync(FlagDataCacheKey(flagName));
        await cache.RemoveAsync(AllFlagDataCacheKey);
    }
}
