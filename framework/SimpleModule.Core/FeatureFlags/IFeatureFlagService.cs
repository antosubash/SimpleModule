namespace SimpleModule.Core.FeatureFlags;

/// <summary>
/// Minimal runtime interface for checking feature flag state.
/// Lives in Core so any module can depend on it without referencing FeatureFlags.Contracts.
/// </summary>
public interface IFeatureFlagService
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
}
