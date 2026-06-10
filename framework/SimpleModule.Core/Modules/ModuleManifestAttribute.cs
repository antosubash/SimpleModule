using System;

namespace SimpleModule.Core.Modules;

/// <summary>
/// Carries the compile-time module manifest JSON emitted by SimpleModule.Generator.
/// Assembly-level so tooling can read it via System.Reflection.Metadata without
/// loading the assembly. Source generators cannot add embedded resources, which is
/// why the manifest travels as an attribute rather than a resource stream.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
public sealed class ModuleManifestAttribute(string json) : Attribute
{
    public string Json { get; } = json;
}
