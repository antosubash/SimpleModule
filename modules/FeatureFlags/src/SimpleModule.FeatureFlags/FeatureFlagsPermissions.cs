using SimpleModule.Core.Authorization;

namespace SimpleModule.FeatureFlags;

public sealed class FeatureFlagsPermissions : IModulePermissions
{
    public const string View = "FeatureFlags.View";
    public const string Manage = "FeatureFlags.Manage";
}
