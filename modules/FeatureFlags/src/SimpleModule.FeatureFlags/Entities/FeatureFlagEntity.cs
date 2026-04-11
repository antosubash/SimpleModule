using SimpleModule.Core.Entities;
using SimpleModule.FeatureFlags.Contracts;

namespace SimpleModule.FeatureFlags.Entities;

public class FeatureFlagEntity : Entity<FeatureFlagId>
{
    public string Name { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public bool IsDeprecated { get; set; }
}
