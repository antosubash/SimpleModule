using Microsoft.Extensions.Logging;
using SimpleModule.AuditLogs.Contracts;
using SimpleModule.Core.Events;
using SimpleModule.Core.Settings;
using SimpleModule.Settings.Contracts;

namespace SimpleModule.AuditLogs.Pipeline;

public sealed class AuditingEventBus(
    IEventBus inner,
    IAuditContext auditContext,
    AuditChannel channel,
    ISettingsContracts? settings = null,
    ILogger<AuditingEventBus>? logger = null
) : IEventBus
{
    public async Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default)
        where T : IEvent
    {
        // Publish to inner event bus FIRST, before attempting audit
        await inner.PublishAsync(@event, cancellationToken);

        // Only audit on successful publish
        var enabled =
            settings is null
            || await settings.GetSettingAsync<bool>("auditlogs.capture.domain", SettingScope.System)
                != false;

        if (enabled)
        {
            try
            {
                var entry = AuditEntryExtractor.Extract(@event, auditContext);
                channel.Enqueue(entry);
            }
            catch (OperationCanceledException)
            {
                // Don't log cancellation, just propagate
                throw;
            }
#pragma warning disable CA1031
            catch (Exception ex)
            {
                // Audit failures must never break primary operations
                // We catch all exceptions because any failure during audit (channel, extraction, etc.)
                // should be logged but not propagated to the caller
                logger?.LogError(ex, "Failed to enqueue audit entry; audit will not be recorded");
            }
#pragma warning restore CA1031
        }
    }

    public void PublishInBackground<T>(T @event)
        where T : IEvent
    {
        inner.PublishInBackground(@event);
    }
}
