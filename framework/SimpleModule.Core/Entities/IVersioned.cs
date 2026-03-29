namespace SimpleModule.Core.Entities;

/// <summary>
/// Entities implementing this interface get an auto-incrementing version number.
/// Starts at 1 on insert, increments on each update. Configured as a concurrency token.
/// </summary>
public interface IVersioned
{
    int Version { get; set; }
}
