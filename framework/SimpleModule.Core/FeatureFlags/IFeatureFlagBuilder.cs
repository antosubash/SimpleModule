namespace SimpleModule.Core.FeatureFlags;

public interface IFeatureFlagBuilder
{
    IFeatureFlagBuilder Add(FeatureFlagDefinition definition);
}
