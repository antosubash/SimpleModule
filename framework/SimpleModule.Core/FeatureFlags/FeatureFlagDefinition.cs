namespace SimpleModule.Core.FeatureFlags;

public class FeatureFlagDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool DefaultEnabled { get; set; }
}
