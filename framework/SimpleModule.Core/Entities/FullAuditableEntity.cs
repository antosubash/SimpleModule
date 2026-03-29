namespace SimpleModule.Core.Entities;

/// <summary>
/// Entity with full audit tracking, soft delete, and versioning.
/// This is the most commonly used base class for business entities.
/// </summary>
public abstract class FullAuditableEntity<TId> : AuditableEntity<TId>, ISoftDelete, IVersioned
{
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    public int Version { get; set; }
}
