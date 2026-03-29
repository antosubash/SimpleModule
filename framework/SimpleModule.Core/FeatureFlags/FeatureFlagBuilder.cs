namespace SimpleModule.Core.FeatureFlags;

public sealed class FeatureFlagBuilder : IFeatureFlagBuilder
{
    private readonly List<FeatureFlagDefinition> _definitions = [];

    public IFeatureFlagBuilder Add(FeatureFlagDefinition definition)
    {
        _definitions.Add(definition);
        return this;
    }

    public List<FeatureFlagDefinition> ToList() => [.. _definitions];
}
