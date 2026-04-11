using SimpleModule.Core.Events;
using SimpleModule.Core.Settings;

namespace SimpleModule.Settings.Contracts.Events;

public sealed record SettingDeletedEvent(string Key, SettingScope Scope) : IEvent;
