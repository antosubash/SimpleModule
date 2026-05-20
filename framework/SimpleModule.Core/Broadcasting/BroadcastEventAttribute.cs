namespace SimpleModule.Core.Broadcasting;

/// <summary>
/// Marks an <see cref="IBroadcastEvent"/> record with the wire-format event
/// name that browser clients subscribe to. The same event type may be reused
/// across modules; the name is what client code listens for via
/// <c>useEvent('orders.created', ...)</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class BroadcastEventAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}
