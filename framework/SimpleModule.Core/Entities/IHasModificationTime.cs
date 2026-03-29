namespace SimpleModule.Core.Entities;

/// <summary>
/// Entities implementing this interface get <see cref="UpdatedAt"/> automatically set on insert and update.
/// </summary>
public interface IHasModificationTime
{
    DateTimeOffset UpdatedAt { get; set; }
}
