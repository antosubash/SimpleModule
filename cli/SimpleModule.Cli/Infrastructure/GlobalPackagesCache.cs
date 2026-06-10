namespace SimpleModule.Cli.Infrastructure;

/// <summary>
/// Reads module manifests out of the NuGet global packages cache
/// (<c>~/.nuget/packages</c> or <c>NUGET_PACKAGES</c>).
/// </summary>
public static class GlobalPackagesCache
{
    public static string RootPath =>
        Environment.GetEnvironmentVariable("NUGET_PACKAGES")
        ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".nuget",
            "packages"
        );

    /// <summary>
    /// Returns the manifest for an installed package, or null when the package
    /// (or a manifest inside it) cannot be found. Without a version the highest
    /// cached one is used.
    /// </summary>
    public static ModuleManifestData? TryReadManifest(string packageId, string? version)
    {
        var packageDir = Path.Combine(RootPath, packageId.ToLowerInvariant());
        if (!Directory.Exists(packageDir))
        {
            return null;
        }

        var versionDirs = version is not null
            ? new[] { Path.Combine(packageDir, version.ToLowerInvariant()) }
            : Directory
                .EnumerateDirectories(packageDir)
                .OrderByDescending(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
                .ToArray();

        foreach (var versionDir in versionDirs)
        {
            if (!Directory.Exists(versionDir))
            {
                continue;
            }

            // sm pack puts module-manifest.json at the package root.
            var manifestPath = Path.Combine(versionDir, "module-manifest.json");
            if (File.Exists(manifestPath))
            {
                var parsed = ModuleManifestData.TryParse(File.ReadAllText(manifestPath));
                if (parsed is not null)
                {
                    return parsed;
                }
            }

            var libDir = Path.Combine(versionDir, "lib");
            if (!Directory.Exists(libDir))
            {
                continue;
            }

            var dll = Directory
                .EnumerateFiles(libDir, packageId + ".dll", SearchOption.AllDirectories)
                .FirstOrDefault();
            if (dll is not null)
            {
                var manifest = AssemblyManifestReader.TryRead(dll);
                if (manifest is not null)
                {
                    return manifest;
                }
            }
        }

        return null;
    }
}
