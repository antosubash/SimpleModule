namespace SimpleModule.FeatureFlags.Contracts;

public class FeatureFlag
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public bool DefaultEnabled { get; set; }
    public bool IsDeprecated { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
