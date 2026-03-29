namespace SimpleModule.Core.Entities;

/// <summary>
/// Entities implementing this interface get <see cref="CreatedAt"/> automatically set on insert.
/// </summary>
public interface IHasCreationTime
{
    DateTimeOffset CreatedAt { get; set; }
}
