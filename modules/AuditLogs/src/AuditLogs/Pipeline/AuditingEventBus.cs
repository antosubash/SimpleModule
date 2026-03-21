using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using SimpleModule.AuditLogs.Contracts;
using SimpleModule.Core.Events;
using SimpleModule.Core.Settings;
using SimpleModule.Settings.Contracts;

namespace SimpleModule.AuditLogs.Pipeline;

public sealed partial class AuditingEventBus(
    IEventBus inner,
    IAuditContext auditContext,
    AuditChannel channel,
    ISettingsContracts? settings = null
) : IEventBus
{
    [GeneratedRegex(
        @"^(?<entity>.+?)(?<action>Created|Updated|Deleted|Viewed|Exported|LoginSuccess|LoginFailed|PermissionGranted|PermissionRevoked|SettingChanged)Event$"
    )]
    private static partial Regex EventNamePattern();

    public async Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default)
        where T : IEvent
    {
        var enabled =
            settings is null
            || await settings.GetSettingAsync<bool>("auditlogs.capture.domain", SettingScope.System)
                != false;

        if (enabled)
        {
            var entry = ExtractAuditEntry(@event);
            channel.Enqueue(entry);
        }

        await inner.PublishAsync(@event, cancellationToken);
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
