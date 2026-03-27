namespace SimpleModule.Core;

/// <summary>
/// Declares the Inertia component name for an <see cref="IViewEndpoint"/>.
/// The generator uses this to validate page registry entries and enforce naming conventions.
/// </summary>
/// <param name="component">
/// The Inertia component name (e.g., "Products/Browse"). Must match the key in Pages/index.ts
/// and the first argument to <c>Inertia.Render()</c>.
/// </param>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ViewPageAttribute(string component) : Attribute
{
    public string Component { get; } = component;
}
