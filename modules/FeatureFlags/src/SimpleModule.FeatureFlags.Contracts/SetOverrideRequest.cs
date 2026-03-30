namespace SimpleModule.FeatureFlags.Contracts;

public class SetOverrideRequest
{
    public OverrideType OverrideType { get; set; }
    public string OverrideValue { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
}
