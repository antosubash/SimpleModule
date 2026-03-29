namespace SimpleModule.Core.Entities;

/// <summary>
/// Entities implementing this interface get full audit tracking:
/// timestamps and the user who created/modified the entity (resolved from the current HTTP context).
/// </summary>
public interface IAuditable : IHasCreationTime, IHasModificationTime
{
    string? CreatedBy { get; set; }
    string? UpdatedBy { get; set; }
}
