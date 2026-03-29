namespace SimpleModule.FeatureFlags.Contracts;

public interface IFeatureFlagContracts
{
    Task<bool> IsEnabledAsync(
        string flagName,
        string? userId = null,
        IEnumerable<string>? roles = null
    );

    Task<Dictionary<string, bool>> GetAllEnabledAsync(
        string? userId = null,
        IEnumerable<string>? roles = null
    );

    Task<IEnumerable<FeatureFlag>> GetAllFlagsAsync();
    Task<FeatureFlag?> GetFlagAsync(string flagName);
    Task<FeatureFlag> UpdateFlagAsync(string flagName, UpdateFeatureFlagRequest request);
    Task<IEnumerable<FeatureFlagOverride>> GetOverridesAsync(string flagName);
    Task<FeatureFlagOverride> SetOverrideAsync(string flagName, SetOverrideRequest request);
    Task DeleteOverrideAsync(int overrideId);
}
