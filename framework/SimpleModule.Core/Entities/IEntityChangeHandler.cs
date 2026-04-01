namespace SimpleModule.Core.Entities;

/// <summary>
/// Typed handler invoked automatically after SaveChanges when an entity of type <typeparamref name="T"/> changes.
/// Useful for cache invalidation, denormalized data updates, or triggering side effects.
/// </summary>
public interface IEntityChangeHandler<T>
    where T : class
{
    Task HandleAsync(EntityChangeContext<T> context, CancellationToken cancellationToken = default);
}
