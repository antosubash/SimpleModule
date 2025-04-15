using System;

namespace SimpleModule.Core;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ModuleAttribute : Attribute
{
    public string Name { get; }
    public string Version { get; }

    public ModuleAttribute(string name, string version = "1.0.0")
    {
        Name = name;
        Version = version;
    }
} 