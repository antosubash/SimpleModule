using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SimpleModule.AuditLogs.Contracts;
using SimpleModule.AuditLogs.Pipeline;
using SimpleModule.Core.Settings;
using SimpleModule.Settings.Contracts;

namespace SimpleModule.AuditLogs.Interceptors;

public sealed class AuditSaveChangesInterceptor(
    IAuditContext auditContext,
    AuditChannel channel,
    ISettingsContracts? settings = null
) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default
    )
    {
        if (eventData.Context is null)
            return base.SavingChangesAsync(eventData, result, cancellationToken);

        if (settings is not null)
        {
            var raw = settings
                .GetSettingAsync("auditlogs.capture.changes", SettingScope.System)
                .GetAwaiter()
                .GetResult();
            if (string.Equals(raw, "false", StringComparison.OrdinalIgnoreCase))
                return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        var contextType = eventData.Context.GetType();

        // Don't audit our own DbContext to avoid infinite loops
        if (contextType == typeof(AuditLogsDbContext))
            return base.SavingChangesAsync(eventData, result, cancellationToken);

        var moduleName = contextType.Name.Replace("DbContext", "", StringComparison.Ordinal);

        foreach (var entry in eventData.Context.ChangeTracker.Entries())
        {
            if (entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            {
                var auditEntry = CreateAuditEntry(entry, moduleName);
                if (auditEntry is not null)
                {
                    channel.Enqueue(auditEntry);
                }
            }
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private AuditEntry? CreateAuditEntry(EntityEntry entry, string moduleName)
    {
        var entityType = entry.Metadata.ClrType.Name;
        var action = entry.State switch
        {
            EntityState.Added => AuditAction.Created,
            EntityState.Modified => AuditAction.Updated,
            EntityState.Deleted => AuditAction.Deleted,
            _ => (AuditAction?)null,
        };

        if (action is null)
            return null;

        string? entityId = null;
        var primaryKey = entry.Metadata.FindPrimaryKey();
        if (primaryKey is not null)
        {
            var keyValues = primaryKey
                .Properties.Select(p => entry.CurrentValues[p]?.ToString())
                .Where(v => v is not null);
            entityId = string.Join(",", keyValues);
        }

        string? changes = null;
        if (entry.State == EntityState.Modified)
        {
            var changedProps = entry
                .Properties.Where(p => p.IsModified)
                .Select(p => new
                {
                    field = p.Metadata.Name,
                    old = p.OriginalValue?.ToString(),
                    @new = p.CurrentValue?.ToString(),
                })
                .ToList();
            if (changedProps.Count > 0)
                changes = JsonSerializer.Serialize(changedProps);
        }
        else if (entry.State == EntityState.Added)
        {
            var props = entry
                .Properties.Where(p => p.CurrentValue is not null)
                .Select(p => new { field = p.Metadata.Name, value = p.CurrentValue?.ToString() })
                .ToList();
            if (props.Count > 0)
                changes = JsonSerializer.Serialize(props);
        }
        else if (entry.State == EntityState.Deleted)
        {
            var props = entry
                .Properties.Where(p => p.OriginalValue is not null)
                .Select(p => new { field = p.Metadata.Name, value = p.OriginalValue?.ToString() })
                .ToList();
            if (props.Count > 0)
                changes = JsonSerializer.Serialize(props);
        }

        return new AuditEntry
        {
            CorrelationId = auditContext.CorrelationId,
            Source = AuditSource.ChangeTracker,
            Timestamp = DateTimeOffset.UtcNow,
            UserId = auditContext.UserId,
            UserName = auditContext.UserName,
            IpAddress = auditContext.IpAddress,
            Module = moduleName,
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            Changes = changes,
        };
    }
}
