namespace SimpleModule.Core.Entities;

/// <summary>
/// Entities implementing this interface have a flexible key-value property bag
/// stored as a JSON column. Useful for extensibility without schema changes.
/// </summary>
public interface IHasExtraProperties
{
    Dictionary<string, object?> ExtraProperties { get; set; }
}
