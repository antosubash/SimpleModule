using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using SimpleModule.AuditLogs.Contracts;
using SimpleModule.Core.Events;

namespace SimpleModule.AuditLogs.Pipeline;

/// <summary>
/// Reflects over an <see cref="IEvent"/> instance to produce a matching
/// <see cref="AuditEntry"/>. Used by <see cref="AuditingMessageBus"/>
/// to build audit entries for every published domain event.
/// </summary>
internal static partial class AuditEntryExtractor
{
    [GeneratedRegex(
        @"^(?<entity>.+?)(?<action>Created|Updated|Deleted|Viewed|Exported|LoginSuccess|LoginFailed|PermissionGranted|PermissionRevoked|SettingChanged)Event$"
    )]
    private static partial Regex EventNamePattern();

    public static AuditEntry Extract(IEvent evt, IAuditContext auditContext)
    {
        var eventType = evt.GetType();
        var typeName = eventType.Name;
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

        var properties = eventType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var prop in properties)
        {
            var value = prop.GetValue(evt);
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
