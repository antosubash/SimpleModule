namespace SimpleModule.Core.Entities;

/// <summary>
/// Entities implementing this interface get a random concurrency stamp set on each save.
/// Configured as a concurrency token for optimistic concurrency control.
/// </summary>
public interface IHasConcurrencyStamp
{
    string ConcurrencyStamp { get; set; }
}
