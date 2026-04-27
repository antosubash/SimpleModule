using SimpleModule.Core.Settings;
using SimpleModule.Settings.Contracts.Events;
using ZiggyCreatures.Caching.Fusion;

namespace SimpleModule.AuditLogs.Pipeline;

internal static class AuditConfigCacheInvalidator
{
    public static ValueTask Handle(SettingChangedEvent @event, IFusionCache cache)
    {
        if (@event.Scope != SettingScope.System)
        {
            return ValueTask.CompletedTask;
        }

        if (!@event.Key.StartsWith("auditlogs.", StringComparison.Ordinal))
        {
            return ValueTask.CompletedTask;
        }

        return cache.RemoveAsync(AuditCacheKeys.RequestConfig);
    }
}
