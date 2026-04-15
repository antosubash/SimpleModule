using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SimpleModule.Core.Entities;
using SimpleModule.Core.FeatureFlags;
using SimpleModule.FeatureFlags.Contracts;
using ZiggyCreatures.Caching.Fusion;

namespace SimpleModule.FeatureFlags;

public sealed partial class FeatureFlagService(
    FeatureFlagsDbContext db,
    IFeatureFlagRegistry registry,
    IFusionCache cache,
    ILogger<FeatureFlagService> logger,
    IServiceProvider serviceProvider
) : IFeatureFlagContracts, IFeatureFlagService
{
    private readonly Lazy<ITenantContext?> _tenantContext = new(() =>
        serviceProvider.GetService<ITenantContext>()
    );
    private static readonly FusionCacheEntryOptions CacheOptions = new()
    {
        Duration = TimeSpan.FromSeconds(30),
    };
    private const string AllFlagDataCacheKey = "ff:all-data";
    private const string FlagDataKeyPrefix = "ff:data:";

    private static string FlagDataCacheKey(string flagName) => FlagDataKeyPrefix + flagName;

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

    public async Task DeleteOverrideAsync(FeatureFlagOverrideId overrideId)
    {
        var entity = await db.FeatureFlagOverrides.FindAsync(overrideId);
        if (entity is not null)
        {
            db.FeatureFlagOverrides.Remove(entity);
            await db.SaveChangesAsync();
            await InvalidateCacheAsync(entity.FlagName);
        }
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Feature flag '{FlagName}' toggled to {IsEnabled}"
    )]
    private static partial void LogFlagToggled(ILogger logger, string flagName, bool isEnabled);
}
