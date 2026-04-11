using SimpleModule.Core;
using SimpleModule.Core.Entities;

namespace SimpleModule.FeatureFlags.Contracts;

[NoDtoGeneration]
public class FeatureFlagEntity : Entity<FeatureFlagId>
{
    public string Name { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public bool IsDeprecated { get; set; }
}
