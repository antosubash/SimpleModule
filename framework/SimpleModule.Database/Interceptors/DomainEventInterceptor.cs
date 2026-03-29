using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core.Entities;
using SimpleModule.Core.Events;

namespace SimpleModule.Database.Interceptors;

/// <summary>
/// Interceptor that collects domain events from <see cref="IHasDomainEvents"/> entities
/// before SaveChanges and dispatches them via <see cref="IEventBus"/> after a successful save.
/// Events are cleared from entities after dispatch to prevent re-processing.
/// Registered as scoped — each DbContext gets its own instance, so instance fields are safe.
/// </summary>
public sealed class DomainEventInterceptor(IServiceProvider serviceProvider) : SaveChangesInterceptor
{
    private static readonly MethodInfo PublishAsyncMethod =
        typeof(IEventBus).GetMethod(nameof(IEventBus.PublishAsync))!;
    private static readonly ConcurrentDictionary<Type, MethodInfo> PublishMethodCache = new();

    private List<IEvent>? _collectedEvents;

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            var events = new List<IEvent>();

            foreach (var entry in eventData.Context.ChangeTracker.Entries<IHasDomainEvents>())
            {
                var domainEvents = entry.Entity.GetDomainEvents();
                if (domainEvents.Count > 0)
                {
                    events.AddRange(domainEvents);
                    entry.Entity.ClearDomainEvents();
                }
            }

            _collectedEvents = events.Count > 0 ? events : null;
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        var events = _collectedEvents;
        _collectedEvents = null;

        if (events is { Count: > 0 })
        {
            var eventBus = serviceProvider.GetService<IEventBus>();
            if (eventBus is not null)
            {
                foreach (var domainEvent in events)
                {
                    // Invoke PublishAsync<T> with the concrete event type so that
                    // IEventHandler<T> registrations are resolved correctly.
                    // A static call to PublishAsync(IEvent) would resolve T as IEvent,
                    // missing all concrete handlers.
                    var concreteMethod = PublishMethodCache.GetOrAdd(
                        domainEvent.GetType(),
                        static type => PublishAsyncMethod.MakeGenericMethod(type));
                    await (Task)concreteMethod.Invoke(eventBus, [domainEvent, cancellationToken])!;
                }
            }
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        _collectedEvents = null;
        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }
}
