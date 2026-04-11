using SimpleModule.Core;
using SimpleModule.Core.Entities;

namespace SimpleModule.FeatureFlags.Contracts;

[NoDtoGeneration]
public class FeatureFlagOverrideEntity : Entity<FeatureFlagOverrideId>
{
    public string FlagName { get; set; } = string.Empty;
    public OverrideType OverrideType { get; set; }
    public string OverrideValue { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
}
