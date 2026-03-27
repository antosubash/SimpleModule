---
outline: deep
---

# EF Core Interceptors

SimpleModule supports EF Core `SaveChangesInterceptor` for cross-cutting concerns like audit logging, soft deletes, and timestamp management. The source generator auto-discovers interceptors and wires them into the DbContext pipeline.

## Overview

A `SaveChangesInterceptor` hooks into the EF Core save pipeline, allowing you to inspect or modify entities before or after they are persisted. Common use cases include:

- Setting `CreatedAt`/`UpdatedAt` timestamps
- Recording audit log entries
- Enforcing business rules before save
- Publishing domain events after save

## The Circular Dependency Problem

When an interceptor depends on a service that itself depends on a `DbContext`, you get a circular dependency that causes a deadlock during DI construction:

```
SaveChangesInterceptor
    → ISettingsContracts (constructor injection)
        → SettingsService
            → SettingsDbContext (deadlock!)
```

This happens because:

1. EF Core resolves all registered `ISaveChangesInterceptor` instances during DbContext options construction
2. If an interceptor's constructor requires a service that transitively depends on a DbContext, the DI container tries to build the DbContext to satisfy the service, which tries to build the interceptors, which tries to build the service...

## The Solution: Lazy Resolution

Inject `IServiceProvider?` as an optional parameter and resolve dependencies at interception time -- not at construction time.

### Correct Pattern

```csharp
public sealed class AuditInterceptor(
    IServiceProvider? serviceProvider = null
) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        // Resolve at interception time -- safe, no circular dependency
        var settings = serviceProvider?.GetService<ISettingsContracts>();
        if (settings is not null)
        {
            // Use the service
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
```

### Anti-Pattern

```csharp
// WRONG: Causes circular dependency during DI construction
public sealed class BadInterceptor(
    ISettingsContracts settings  // Don't do this!
) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        // settings was injected in constructor -- this triggers circular dependency
        var value = settings.GetValue("key");
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
```

## Guidelines

### Constructor Parameters

- **Never** inject services that transitively depend on a DbContext into the interceptor constructor
- **Do** inject `IServiceProvider?` as an optional dependency when runtime service resolution is needed
- **Do** inject simple services (like `ILogger<T>`, `TimeProvider`) that have no DbContext dependency

### Service Resolution Timing

- **Only** resolve services within interception methods: `SavingChangesAsync`, `SavedChangesAsync`, or `SaveChangesFailedAsync`
- The framework resolves all registered `ISaveChangesInterceptor` instances **lazily** during DbContext options construction

### Null Safety

Making `IServiceProvider?` optional (with `= null`) ensures the interceptor works in unit tests where DI may not be available:

```csharp
public sealed class TimestampInterceptor(
    IServiceProvider? serviceProvider = null
) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null)
            return base.SavingChangesAsync(eventData, result, cancellationToken);

        var now = serviceProvider?.GetService<TimeProvider>()?.GetUtcNow()
            ?? DateTimeOffset.UtcNow;

        foreach (var entry in eventData.Context.ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Added && entry.Entity is IHasCreatedAt created)
            {
                created.CreatedAt = now;
            }

            if (entry.State == EntityState.Modified && entry.Entity is IHasUpdatedAt updated)
            {
                updated.UpdatedAt = now;
            }
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
```

## Auto-Discovery

The source generator automatically discovers classes that implement `SaveChangesInterceptor` (or `ISaveChangesInterceptor`) in module assemblies. Discovered interceptors are registered in the generated DbContext configuration -- you do not need to manually register them.

The generator records each interceptor's constructor parameters to ensure correct DI wiring:

```csharp
// From DiscoveryData
internal readonly record struct InterceptorInfoRecord(
    string FullyQualifiedName,
    string ModuleName,
    ImmutableArray<string> ConstructorParamTypeFqns
);
```

## Summary

| Do | Don't |
|----|-------|
| Inject `IServiceProvider?` as optional | Inject services that depend on DbContext |
| Resolve services inside interception methods | Resolve services in the constructor |
| Use `?.GetService<T>()` for null safety | Assume services are always available |
| Keep constructors minimal | Add complex initialization logic to constructors |

## Next Steps

- [Deployment](/advanced/deployment) -- production configuration and CI/CD pipeline
- [Database](/guide/database) -- module database contexts and schema isolation
- [Configuration Reference](/reference/configuration) -- all framework settings
