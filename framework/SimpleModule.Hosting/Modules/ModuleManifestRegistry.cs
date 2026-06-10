using SimpleModule.Core;
using SimpleModule.Core.Modules;

namespace SimpleModule.Hosting.Modules;

/// <summary>
/// Builds the manifest registry from the registered <see cref="IModule"/>
/// instances by reading each module assembly's <see cref="ModuleManifestAttribute"/>.
/// </summary>
public sealed class ModuleManifestRegistry : IModuleManifestRegistry
{
    private readonly Dictionary<string, ModuleManifest> _byName;

    public ModuleManifestRegistry(IEnumerable<IModule> modules)
    {
        ArgumentNullException.ThrowIfNull(modules);

        _byName = new Dictionary<string, ModuleManifest>(StringComparer.Ordinal);
        var seenAssemblies = new HashSet<string>(StringComparer.Ordinal);
        foreach (var module in modules)
        {
            var assembly = module.GetType().Assembly;
            if (!seenAssemblies.Add(assembly.FullName ?? assembly.GetName().Name ?? ""))
                continue;

            ModuleManifest? manifest;
            try
            {
                manifest = ModuleManifestReader.TryRead(assembly);
            }
            catch (ModuleManifestException)
            {
                // One unreadable manifest (newer schemaVersion, corrupt JSON) must
                // not take down every page render — the module simply behaves like
                // a pre-manifest module and falls back to convention resolution.
                continue;
            }

            if (manifest is not null && !_byName.ContainsKey(manifest.Name))
                _byName[manifest.Name] = manifest;
        }

        Manifests = [.. _byName.Values];
    }

    public IReadOnlyList<ModuleManifest> Manifests { get; }

    public ModuleManifest? Get(string moduleName) =>
        _byName.TryGetValue(moduleName, out var manifest) ? manifest : null;
}
