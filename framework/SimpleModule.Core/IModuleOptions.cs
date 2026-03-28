namespace SimpleModule.Core;

/// <summary>
/// Marker interface for module options classes. Implementations are auto-discovered
/// by the source generator, registered with <c>IOptions&lt;T&gt;</c>, and exposed as
/// typed <c>Configure{Module}()</c> methods on <c>SimpleModuleOptions</c>.
/// </summary>
/// <remarks>
/// Each module may define at most one options class implementing this interface.
/// The host application can then configure module behavior at startup:
/// <code>
/// builder.AddSimpleModule(o =&gt;
/// {
///     o.ConfigureProducts(p =&gt; p.MaxPageSize = 50);
/// });
/// </code>
/// Module code reads configured values via <c>IOptions&lt;TOptions&gt;</c> injection.
/// </remarks>
#pragma warning disable CA1040 // Avoid empty interfaces
public interface IModuleOptions;
#pragma warning restore CA1040
