using SimpleModule.Core.Entities;
using SimpleModule.FeatureFlags.Contracts;

namespace SimpleModule.FeatureFlags.Entities;

public class FeatureFlagOverrideEntity : Entity<FeatureFlagOverrideId>
{
    public string FlagName { get; set; } = string.Empty;
    public OverrideType OverrideType { get; set; }
    public string OverrideValue { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
}
