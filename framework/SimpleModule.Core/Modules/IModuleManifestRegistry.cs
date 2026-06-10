using System.Collections.Generic;

namespace SimpleModule.Core.Modules;

/// <summary>
/// Runtime access to the compile-time manifests of all loaded modules.
/// Modules compiled before manifest emission existed simply have no entry.
/// </summary>
public interface IModuleManifestRegistry
{
    IReadOnlyList<ModuleManifest> Manifests { get; }

    /// <summary>Returns the manifest for the given module name, or <c>null</c> when absent.</summary>
    ModuleManifest? Get(string moduleName);
}
