using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SimpleModule.Core.Entities;
using SimpleModule.Core.Extensions;
using SimpleModule.Database.SoftDelete;

namespace SimpleModule.Database.Interceptors;

/// <summary>
/// Interceptor that automatically populates entity fields based on implemented interfaces:
/// <list type="bullet">
///   <item><see cref="IHasCreationTime"/> — sets <c>CreatedAt</c> on insert</item>
///   <item><see cref="IHasModificationTime"/> — sets <c>UpdatedAt</c> on insert and update</item>
///   <item><see cref="IAuditable"/> — sets <c>CreatedBy</c>/<c>UpdatedBy</c> from the current user</item>
///   <item><see cref="ISoftDelete"/> — converts hard delete to soft delete</item>
///   <item><see cref="IVersioned"/> — auto-increments <c>Version</c></item>
///   <item><see cref="IHasConcurrencyStamp"/> — sets a new random concurrency stamp</item>
///   <item><see cref="IMultiTenant"/> — sets <c>TenantId</c> from <see cref="ITenantContext"/></item>
/// </list>
/// </summary>
public sealed class EntityInterceptor(
    IHttpContextAccessor httpContextAccessor,
    ITenantContext? tenantContext = null
) : SaveChangesInterceptor
{
    // EF calls the sync SavingChanges for DbContext.SaveChanges() and the async
    // SavingChangesAsync for SaveChangesAsync(); it never cross-calls. Both must run
    // the same stamping logic or a sync save would hard-delete soft-delete entities
    // and skip audit/tenant/concurrency fields silently.
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result
    )
    {
        ApplyChanges(eventData);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default
    )
    {
        ApplyChanges(eventData);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyChanges(DbContextEventData eventData)
    {
        if (eventData.Context is null)
            return;

        var userId = httpContextAccessor.HttpContext?.User?.GetUserId();
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in eventData.Context.ChangeTracker.Entries())
        {
            if (
                entry.State
                is not (EntityState.Added or EntityState.Modified or EntityState.Deleted)
            )
                continue;

            switch (entry.State)
            {
                case EntityState.Added:
                    SetCreationFields(entry, now, userId);
                    SetModificationFields(entry, now, userId);
                    SetConcurrencyFields(entry);
                    SetTenantId(entry);
                    if (entry.Entity is IVersioned v)
                        v.Version = 1;
                    break;

                case EntityState.Modified:
                    SetModificationFields(entry, now, userId);
                    SetConcurrencyFields(entry);
                    if (entry.Entity is IVersioned v2)
                        v2.Version++;
                    break;

                case EntityState.Deleted when entry.Entity is ISoftDelete sd:
                    if (ForceDeleteExtensions.IsMarked(eventData.Context, sd))
                    {
                        ForceDeleteExtensions.Consume(eventData.Context, sd);
                        break;
                    }
                    entry.State = EntityState.Modified;
                    sd.IsDeleted = true;
                    sd.DeletedAt = now;
                    sd.DeletedBy = userId;
                    SetModificationFields(entry, now, userId);
                    SetConcurrencyFields(entry);
                    if (entry.Entity is IVersioned v3)
                        v3.Version++;
                    break;
            }
        }
    }

    private static void SetCreationFields(EntityEntry entry, DateTimeOffset now, string? userId)
    {
        // Creation fields are stamped once and then preserved. Only default them
        // when the caller hasn't set a value, so an explicitly-assigned creator
        // (e.g. a background job creating a row on behalf of a user, where there is
        // no HttpContext) isn't clobbered with the ambient — possibly null — user (#230).
        if (entry.Entity is IHasCreationTime c && c.CreatedAt == default)
            c.CreatedAt = now;

        if (entry.Entity is IAuditable a && string.IsNullOrEmpty(a.CreatedBy))
            a.CreatedBy = userId;
    }

    private static void SetModificationFields(EntityEntry entry, DateTimeOffset now, string? userId)
    {
        if (entry.Entity is IHasModificationTime m)
            m.UpdatedAt = now;

        if (entry.Entity is IAuditable a)
            a.UpdatedBy = userId;
    }

    private static void SetConcurrencyFields(EntityEntry entry)
    {
        if (entry.Entity is IHasConcurrencyStamp cs)
            cs.ConcurrencyStamp = Guid.NewGuid().ToString("N");
    }

    private void SetTenantId(EntityEntry entry)
    {
        if (entry.Entity is IMultiTenant mt && tenantContext?.TenantId is not null)
            mt.TenantId = tenantContext.TenantId;
    }
}
