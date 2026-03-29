using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SimpleModule.Core.Entities;

namespace SimpleModule.Database.Interceptors;

/// <summary>
/// Interceptor that captures entity changes before SaveChanges and dispatches them to
/// typed <see cref="IEntityChangeHandler{T}"/> implementations after a successful save.
/// Registered as scoped — each DbContext gets its own instance, so instance fields are safe.
/// Caches compiled delegates per entity type to avoid per-save reflection overhead.
/// </summary>
public sealed class EntityChangeInterceptor(
    IServiceProvider serviceProvider,
    ILogger<EntityChangeInterceptor> logger
) : SaveChangesInterceptor
{
    private static readonly ConcurrentDictionary<Type, HandlerMetadata> MetadataCache = new();

    private List<(object Entity, EntityChangeType ChangeType)>? _capturedChanges;

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            var changes = new List<(object Entity, EntityChangeType ChangeType)>();

            foreach (var entry in eventData.Context.ChangeTracker.Entries())
            {
                if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
                    continue;

                var changeType = entry.State switch
                {
                    EntityState.Added => EntityChangeType.Created,
                    EntityState.Deleted => EntityChangeType.Deleted,
                    EntityState.Modified when entry.Entity is ISoftDelete { IsDeleted: true }
                        && entry.Property(nameof(ISoftDelete.IsDeleted)).IsModified
                        => EntityChangeType.Deleted,
                    _ => EntityChangeType.Updated,
                };

                changes.Add((entry.Entity, changeType));
            }

            _capturedChanges = changes.Count > 0 ? changes : null;
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        var changes = _capturedChanges;
        _capturedChanges = null;

        if (changes is { Count: > 0 })
        {
            foreach (var (entity, changeType) in changes)
            {
                await DispatchToHandlersAsync(entity, changeType, cancellationToken);
            }
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        _capturedChanges = null;
        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    private async Task DispatchToHandlersAsync(
        object entity,
        EntityChangeType changeType,
        CancellationToken cancellationToken)
    {
        var entityType = entity.GetType();
        var metadata = MetadataCache.GetOrAdd(entityType, BuildMetadata);
        var handlers = serviceProvider.GetServices(metadata.HandlerType);

        foreach (var handler in handlers)
        {
            try
            {
                var context = metadata.ContextFactory(entity, changeType);
                await metadata.Invoker(handler!, context, cancellationToken);
            }
#pragma warning disable CA1031 // Handlers should not crash the save pipeline
            catch (Exception ex)
#pragma warning restore CA1031
            {
                logger.LogError(
                    ex,
                    "Entity change handler {HandlerType} failed for {EntityType} ({ChangeType})",
                    handler?.GetType().Name,
                    entityType.Name,
                    changeType);
            }
        }
    }

    private static HandlerMetadata BuildMetadata(Type entityType)
    {
        var handlerType = typeof(IEntityChangeHandler<>).MakeGenericType(entityType);
        var contextType = typeof(EntityChangeContext<>).MakeGenericType(entityType);

        // Compile a factory delegate: (object entity, EntityChangeType ct) => new EntityChangeContext<T>((T)entity, ct)
        var entityParam = Expression.Parameter(typeof(object), "entity");
        var changeTypeParam = Expression.Parameter(typeof(EntityChangeType), "changeType");
        var ctor = contextType.GetConstructor([entityType, typeof(EntityChangeType)])!;
        var newExpr = Expression.New(ctor, Expression.Convert(entityParam, entityType), changeTypeParam);
        var contextFactory = Expression.Lambda<Func<object, EntityChangeType, object>>(
            Expression.Convert(newExpr, typeof(object)),
            entityParam, changeTypeParam).Compile();

        // Compile an invoker delegate: (object handler, object ctx, CancellationToken ct) => ((IEntityChangeHandler<T>)handler).HandleAsync((EntityChangeContext<T>)ctx, ct)
        var handlerParam = Expression.Parameter(typeof(object), "handler");
        var ctxParam = Expression.Parameter(typeof(object), "ctx");
        var ctParam = Expression.Parameter(typeof(CancellationToken), "ct");
        var handleMethod = handlerType.GetMethod(nameof(IEntityChangeHandler<object>.HandleAsync))!;
        var callExpr = Expression.Call(
            Expression.Convert(handlerParam, handlerType),
            handleMethod,
            Expression.Convert(ctxParam, contextType),
            ctParam);
        var invoker = Expression.Lambda<Func<object, object, CancellationToken, Task>>(
            callExpr, handlerParam, ctxParam, ctParam).Compile();

        return new HandlerMetadata(handlerType, contextFactory, invoker);
    }

    private sealed record HandlerMetadata(
        Type HandlerType,
        Func<object, EntityChangeType, object> ContextFactory,
        Func<object, object, CancellationToken, Task> Invoker);
}
