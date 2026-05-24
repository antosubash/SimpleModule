using SimpleModule.Core.FeatureFlags;

namespace SimpleModule.FeatureFlags.Contracts;

public sealed class FeatureFlagsFeatures : IModuleFeatures
{
    public const string OverrideManagement = "FeatureFlags.OverrideManagement";
}
