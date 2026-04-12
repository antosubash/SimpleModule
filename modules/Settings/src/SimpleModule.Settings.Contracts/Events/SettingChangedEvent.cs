using SimpleModule.Core.Events;
using SimpleModule.Core.Settings;

namespace SimpleModule.Settings.Contracts.Events;

public sealed record SettingChangedEvent(
    string Key,
    string? OldValue,
    string? NewValue,
    SettingScope Scope
) : IEvent;
