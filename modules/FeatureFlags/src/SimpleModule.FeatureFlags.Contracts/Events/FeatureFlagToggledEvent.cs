using SimpleModule.Core.Events;

namespace SimpleModule.FeatureFlags.Contracts.Events;

public sealed record FeatureFlagToggledEvent(string FlagName, bool IsEnabled, string UserId)
    : IEvent;
