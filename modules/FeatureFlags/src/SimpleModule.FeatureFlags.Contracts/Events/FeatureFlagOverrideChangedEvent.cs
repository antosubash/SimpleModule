using SimpleModule.Core.Events;

namespace SimpleModule.FeatureFlags.Contracts.Events;

public enum OverrideAction
{
    Set,
    Delete,
}

public sealed record FeatureFlagOverrideChangedEvent(
    string FlagName,
    OverrideAction Action,
    OverrideType OverrideType,
    string OverrideValue
) : IEvent;
