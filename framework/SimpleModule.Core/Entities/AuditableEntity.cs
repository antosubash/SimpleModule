namespace SimpleModule.Core.Entities;

/// <summary>
/// Entity with full audit tracking: timestamps and the user who created/modified the entity.
/// </summary>
public abstract class AuditableEntity<TId> : Entity<TId>, IAuditable
{
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}
