namespace SimpleModule.Core.Entities;

/// <summary>
/// Entities implementing this interface are soft-deleted instead of hard-deleted.
/// When EF Core <c>Remove()</c> is called, the entity state is changed to Modified and
/// the soft-delete fields are set automatically. A global query filter excludes soft-deleted entities.
/// </summary>
public interface ISoftDelete
{
    bool IsDeleted { get; set; }
    DateTimeOffset? DeletedAt { get; set; }
    string? DeletedBy { get; set; }
}
