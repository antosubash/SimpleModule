namespace SimpleModule.FeatureFlags.Contracts;

public class FeatureFlagOverride
{
    public int Id { get; set; }
    public string FlagName { get; set; } = string.Empty;
    public OverrideType OverrideType { get; set; }
    public string OverrideValue { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
}
