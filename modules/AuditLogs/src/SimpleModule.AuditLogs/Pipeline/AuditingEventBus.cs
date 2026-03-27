using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using SimpleModule.AuditLogs.Contracts;
using SimpleModule.Core.Events;
using SimpleModule.Core.Settings;
using SimpleModule.Settings.Contracts;

namespace SimpleModule.AuditLogs.Pipeline;

public sealed partial class AuditingEventBus(
    IEventBus inner,
    IAuditContext auditContext,
    AuditChannel channel,
    ISettingsContracts? settings = null,
    ILogger<AuditingEventBus>? logger = null
) : IEventBus
{
    [GeneratedRegex(
        @"^(?<entity>.+?)(?<action>Created|Updated|Deleted|Viewed|Exported|LoginSuccess|LoginFailed|PermissionGranted|PermissionRevoked|SettingChanged)Event$"
    )]
    private static partial Regex EventNamePattern();

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
                var entry = ExtractAuditEntry(@event);
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

    private AuditEntry ExtractAuditEntry<T>(T @event)
        where T : IEvent
    {
        var typeName = typeof(T).Name;
        var match = EventNamePattern().Match(typeName);

        string? module = null;
        AuditAction action = AuditAction.Other;
        string? entityType = null;
        string? entityId = null;
        Dictionary<string, object?>? metadata = null;

        if (match.Success)
        {
            entityType = match.Groups["entity"].Value;
            module = entityType;
            if (Enum.TryParse<AuditAction>(match.Groups["action"].Value, out var parsed))
            {
                action = parsed;
            }
        }

        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var prop in properties)
        {
            var value = prop.GetValue(@event);
            if (prop.Name.EndsWith("Id", StringComparison.Ordinal) && value is not null)
            {
                entityId ??= value.ToString();
                if (entityType is null && prop.Name.Length > 2)
                {
                    entityType = prop.Name[..^2];
                    module ??= entityType;
                }
            }
            else if (value is not null)
            {
                metadata ??= [];
                metadata[prop.Name] = value;
            }
        }

        return new AuditEntry
        {
            CorrelationId = auditContext.CorrelationId,
            Source = AuditSource.Domain,
            Timestamp = DateTimeOffset.UtcNow,
            UserId = auditContext.UserId,
            UserName = auditContext.UserName,
            IpAddress = auditContext.IpAddress,
            Module = module,
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            Metadata = metadata is not null ? JsonSerializer.Serialize(metadata) : null,
        };
    }
}
