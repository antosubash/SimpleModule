using SimpleModule.Core.Entities;

namespace SimpleModule.FeatureFlags.Entities;

public class FeatureFlagEntity : Entity<int>
{
    public string Name { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public bool IsDeprecated { get; set; }
}
