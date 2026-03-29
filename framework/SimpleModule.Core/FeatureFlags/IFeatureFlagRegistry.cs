namespace SimpleModule.Core.FeatureFlags;

public interface IFeatureFlagRegistry
{
    FeatureFlagDefinition? GetDefinition(string name);
    IReadOnlyList<FeatureFlagDefinition> GetAllDefinitions();
    bool IsKnownFeature(string name);
    IReadOnlySet<string> GetAllFeatureNames();
}
