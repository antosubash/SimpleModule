namespace SimpleModule.Core.Entities;

/// <summary>
/// Base entity with a strongly-typed ID, creation/modification timestamps, and a concurrency stamp.
/// </summary>
public abstract class Entity<TId> : IHasCreationTime, IHasModificationTime, IHasConcurrencyStamp
{
    public TId Id { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string ConcurrencyStamp { get; set; } = string.Empty;
}
