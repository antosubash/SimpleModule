using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.AuditLogs.Contracts;
using SimpleModule.AuditLogs.Pipeline;
using SimpleModule.Core.Settings;
using SimpleModule.Settings.Contracts;

namespace SimpleModule.AuditLogs.Interceptors;

/// <summary>
/// Interceptor that captures entity changes for audit logging.
/// Resolves settings at interception time to avoid circular dependency
/// with DbContext.
/// </summary>
public sealed class AuditSaveChangesInterceptor(
    IAuditContext auditContext,
    AuditChannel channel,
    IServiceProvider? serviceProvider = null
) : SaveChangesInterceptor
{
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default
    )
    {
        if (eventData.Context is null)
            return await base.SavingChangesAsync(eventData, result, cancellationToken);

        // Resolve settings at interception time to avoid circular dependency:
        // SaveChangesInterceptor -> ISettingsContracts -> SettingsService -> DbContext
        // The serviceProvider is injected into the interceptor constructor via DI
        // and used here rather than resolving ISettingsContracts at constructor time
        ISettingsContracts? settings = null;
        if (serviceProvider is not null)
        {
            settings = serviceProvider.GetService<ISettingsContracts>();
        }
        if (settings is not null)
        {
            var raw = await settings.GetSettingAsync("auditlogs.capture.changes", SettingScope.System);
            if (string.Equals(raw, "false", StringComparison.OrdinalIgnoreCase))
                return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        var contextType = eventData.Context.GetType();

        // Don't audit our own DbContext to avoid infinite loops
        if (contextType == typeof(AuditLogsDbContext))
            return await base.SavingChangesAsync(eventData, result, cancellationToken);

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

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
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

        var changes = ExtractPropertyChanges(entry);

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

    private static string? ExtractPropertyChanges(EntityEntry entry)
    {
        var changeSet = entry.State switch
        {
            EntityState.Modified => entry
                .Properties.Where(p => p.IsModified)
                .Select(p => new
                {
                    field = p.Metadata.Name,
                    old = p.OriginalValue?.ToString(),
                    @new = p.CurrentValue?.ToString(),
                })
                .Cast<object>()
                .ToList(),
            EntityState.Added => entry
                .Properties.Where(p => p.CurrentValue is not null)
                .Select(p => new { field = p.Metadata.Name, value = p.CurrentValue?.ToString() })
                .Cast<object>()
                .ToList(),
            EntityState.Deleted => entry
                .Properties.Where(p => p.OriginalValue is not null)
                .Select(p => new { field = p.Metadata.Name, value = p.OriginalValue?.ToString() })
                .Cast<object>()
                .ToList(),
            _ => [],
        };

        return changeSet.Count > 0 ? JsonSerializer.Serialize(changeSet) : null;
    }
}
