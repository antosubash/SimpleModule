namespace SimpleModule.Core.Entities;

/// <summary>
/// Context passed to <see cref="IEntityChangeHandler{T}"/> when an entity change is detected after SaveChanges.
/// </summary>
public sealed record EntityChangeContext<T>(T Entity, EntityChangeType ChangeType)
    where T : class;
