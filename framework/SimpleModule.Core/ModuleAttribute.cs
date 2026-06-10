using System;

namespace SimpleModule.Core;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ModuleAttribute : Attribute
{
    public string Name { get; }
    public string Version { get; }
    public string RoutePrefix { get; set; } = "";
    public string ViewPrefix { get; set; } = "";

    /// <summary>Human-readable module name for UIs and the module manifest. Defaults to <see cref="Name"/>.</summary>
    public string DisplayName { get; set; } = "";

    public ModuleAttribute(string name, string version = "1.0.0")
    {
        Name = name;
        Version = version;
    }
}
