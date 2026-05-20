---
outline: deep
---

# Soft Delete

SimpleModule's soft-delete surface mirrors Laravel's `SoftDeletes` trait. Entities that implement `ISoftDelete` are automatically excluded from queries by a global query filter and are kept in the database when `Remove()` is called. The framework also exposes a recovery surface — `WithTrashed`, `OnlyTrashed`, `Restore`, `ForceDelete` — so admin restore UIs, audit flows, and retention policies do not have to leak the abstraction by reaching for `IgnoreQueryFilters()`.

## ISoftDelete

```csharp
public interface ISoftDelete
{
    bool IsDeleted { get; set; }
    DateTimeOffset? DeletedAt { get; set; }
    string? DeletedBy { get; set; }
}
```

`FullAuditableEntity<TId>` implements this for you. When you call `dbContext.Remove(entity)` on an `ISoftDelete` entity, the `EntityInterceptor` converts the delete to an update that sets `IsDeleted=true`, `DeletedAt=UtcNow`, and `DeletedBy` to the current user. A real `DELETE` statement is never issued unless you opt into [force delete](#force-delete).

## Querying Trashed Rows

By default, soft-deleted rows are hidden from every `DbSet<T>` query.

```csharp
public static class SoftDeleteQueryExtensions
{
    public static IQueryable<T> WithTrashed<T>(this IQueryable<T> q)
        where T : class, ISoftDelete;

    public static IQueryable<T> OnlyTrashed<T>(this IQueryable<T> q)
        where T : class, ISoftDelete;
}
```

| Extension | Result |
|-----------|--------|
| (default)         | Live rows only — soft-delete filter applied |
| `WithTrashed()`   | Live + trashed rows |
| `OnlyTrashed()`   | Trashed rows only |

```csharp
// Live rows
var customers = await db.Customers.ToListAsync();

// Live + trashed
var all = await db.Customers.WithTrashed().ToListAsync();

// Trashed only — for an admin "Recently Deleted" view
var trashed = await db.Customers
    .OnlyTrashed()
    .OrderByDescending(c => c.DeletedAt)
    .Take(50)
    .ToListAsync();
```

::: tip Multi-tenant safety
`WithTrashed` / `OnlyTrashed` ignore only the soft-delete filter. The multi-tenant filter (and any other named filter) stays active, so tenant isolation is preserved when admins browse the trash.
:::

## ISoftDeleteService\<T\>

For per-row recovery operations, register `ISoftDeleteService<T>` and inject it into your endpoints or services.

```csharp
public interface ISoftDeleteService<T> where T : class, ISoftDelete
{
    Task<int> RestoreAsync(object id, CancellationToken ct = default);
    Task<int> ForceDeleteAsync(object id, CancellationToken ct = default);
    Task<int> ForceDeleteRangeAsync(IEnumerable<object> ids, CancellationToken ct = default);
    Task<int> PurgeOlderThanAsync(TimeSpan age, CancellationToken ct = default);
}
```

Register one per soft-deletable entity in the owning module's `ConfigureServices`:

```csharp
public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    services.AddModuleDbContext<CustomersDbContext>(configuration, "Customers");
    services.AddSoftDelete<Customer, CustomersDbContext>();
}
```

`AddSoftDelete<T, TContext>()` registers a scoped `ISoftDeleteService<T>` backed by the supplied `DbContext`.

## Restore

```csharp
app.MapPost("/customers/{id:int}/restore",
    (int id, ISoftDeleteService<Customer> svc, CancellationToken ct) =>
        CrudEndpoints.Restore(() => svc.RestoreAsync(id, ct)));
```

Returns `204 No Content` if a trashed row was restored, `404 Not Found` if the id doesn't match a trashed row.

`RestoreAsync` clears `IsDeleted`, `DeletedAt`, and `DeletedBy`. The `EntityInterceptor` writes a fresh `UpdatedAt`/`UpdatedBy` for the row.

## Force Delete

When you genuinely need to remove a row from the database — GDPR purges, test cleanup, or admin-initiated permanent deletion — call the `ForceDelete` extension instead of `Remove`:

```csharp
db.ForceDelete(customer);          // single
db.ForceDeleteRange(customers);    // batch
await db.SaveChangesAsync();
```

The interceptor consults a per-context marker set, recognises the entry as opted-out of soft delete, and lets the `DELETE` go through. The marker is consumed on observation, so it never leaks across calls.

For id-based force delete, use the service:

```csharp
await svc.ForceDeleteAsync(customerId, ct);
await svc.ForceDeleteRangeAsync([id1, id2, id3], ct);
```

`ForceDeleteAsync` finds the row regardless of trashed state, so it works on both live and already-trashed records.

## Retention / Purge Job

`PurgeOlderThanAsync(age)` permanently deletes every trashed row whose `DeletedAt` is older than the given window. Wire it up as a recurring job using the `BackgroundJobs` module:

```csharp
public sealed class PurgeOldCustomersJob(
    ISoftDeleteService<Customer> softDelete,
    ISettingsContracts settings
) : IModuleJob
{
    public async Task ExecuteAsync(IJobExecutionContext context, CancellationToken ct)
    {
        var raw = await settings.GetSettingAsync("SoftDelete.Customer.RetainDays", SettingScope.System);
        var days = int.TryParse(raw, out var d) ? d : 90;
        var purged = await softDelete.PurgeOlderThanAsync(TimeSpan.FromDays(days), ct);
        context.Log($"Purged {purged} customer rows older than {days} days");
    }
}

// In your module's ConfigureServices
services.AddModuleJob<PurgeOldCustomersJob>();

// On startup, schedule it
await backgroundJobs.AddRecurringAsync<PurgeOldCustomersJob>(
    name: "Purge old customers",
    cronExpression: "0 3 * * *");  // every day at 03:00
```

The retention window is read from a per-entity setting (`SoftDelete.{Entity}.RetainDays`), so operators can tune it without redeploying.

## End-to-End Example

```csharp
// Endpoint module
public sealed class RestoreCustomerEndpoint : IEndpoint
{
    public void Configure(RouteGroupBuilder group)
    {
        group.MapPost("/{id:int}/restore",
            (int id, ISoftDeleteService<Customer> svc, CancellationToken ct) =>
                CrudEndpoints.Restore(() => svc.RestoreAsync(id, ct)))
            .RequireAuthorization("Customers.Restore");
    }
}

public sealed class ForceDeleteCustomerEndpoint : IEndpoint
{
    public void Configure(RouteGroupBuilder group)
    {
        group.MapDelete("/{id:int}/force",
            (int id, ISoftDeleteService<Customer> svc, CancellationToken ct) =>
                CrudEndpoints.ForceDelete(() => svc.ForceDeleteAsync(id, ct)))
            .RequireAuthorization("Customers.ForceDelete");
    }
}
```

## Reference

| Symbol | Purpose |
|--------|---------|
| `WithTrashed<T>()` | Include soft-deleted rows in a query |
| `OnlyTrashed<T>()` | Return only soft-deleted rows |
| `DbContext.ForceDelete(entity)` | Issue a real DELETE for one row |
| `DbContext.ForceDeleteRange(entities)` | Issue real DELETEs for many rows |
| `ISoftDeleteService<T>.RestoreAsync(id)` | Clear soft-delete fields by id |
| `ISoftDeleteService<T>.ForceDeleteAsync(id)` | Hard delete by id |
| `ISoftDeleteService<T>.ForceDeleteRangeAsync(ids)` | Batch hard delete |
| `ISoftDeleteService<T>.PurgeOlderThanAsync(age)` | Retention sweep |
| `CrudEndpoints.Restore` | Endpoint helper, 204/404 |
| `CrudEndpoints.ForceDelete` | Endpoint helper, 204/404 |

## Next Steps

- [Database](/guide/database) -- query filters, schema isolation, and DbContext wiring
- [Background Jobs](/guide/background-jobs) -- scheduling the retention sweep
- [EF Core Interceptors](/advanced/interceptors) -- how the soft-delete interceptor is wired in
