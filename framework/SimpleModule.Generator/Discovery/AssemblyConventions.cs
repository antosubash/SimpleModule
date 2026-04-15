using System;

namespace SimpleModule.Generator;

/// <summary>
/// Naming conventions for SimpleModule assemblies. Centralised so the same
/// string literals don't drift between discovery code and diagnostic emission.
/// </summary>
internal static class AssemblyConventions
{
    internal const string FrameworkPrefix = "SimpleModule.";
    internal const string ContractsSuffix = ".Contracts";
    internal const string ModuleSuffix = ".Module";

    /// <summary>
    /// Derives the `.Contracts` sibling assembly name for a SimpleModule
    /// implementation assembly. Strips a trailing <c>.Module</c> suffix first
    /// so <c>SimpleModule.Agents.Module</c> maps to
    /// <c>SimpleModule.Agents.Contracts</c> instead of
    /// <c>SimpleModule.Agents.Module.Contracts</c>.
    /// </summary>
    internal static string GetExpectedContractsAssemblyName(string implementationAssemblyName)
    {
        var baseName = implementationAssemblyName.EndsWith(ModuleSuffix, StringComparison.Ordinal)
            ? implementationAssemblyName.Substring(
                0,
                implementationAssemblyName.Length - ModuleSuffix.Length
            )
            : implementationAssemblyName;
        return baseName + ContractsSuffix;
    }
}
