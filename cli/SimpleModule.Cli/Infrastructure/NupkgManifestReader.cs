using System.IO.Compression;

namespace SimpleModule.Cli.Infrastructure;

/// <summary>
/// Extracts the module manifest from a nupkg: prefers the
/// <c>module-manifest.json</c> file at the package root (written by
/// <c>sm pack</c>), falling back to the <c>[assembly: ModuleManifest]</c>
/// attribute on the package's main assembly.
/// </summary>
public static class NupkgManifestReader
{
    public static ModuleManifestData? TryRead(string nupkgPath, string packageId)
    {
        if (!File.Exists(nupkgPath))
        {
            return null;
        }

        using var zip = ZipFile.OpenRead(nupkgPath);

        var manifestEntry = zip.GetEntry("module-manifest.json");
        if (manifestEntry is not null)
        {
            using var reader = new StreamReader(manifestEntry.Open());
            var parsed = ModuleManifestData.TryParse(reader.ReadToEnd());
            if (parsed is not null)
            {
                return parsed;
            }
        }

        var dllName = packageId + ".dll";
        var dllEntry = zip.Entries.FirstOrDefault(e =>
            e.FullName.StartsWith("lib/", StringComparison.OrdinalIgnoreCase)
            && string.Equals(e.Name, dllName, StringComparison.OrdinalIgnoreCase)
        );
        if (dllEntry is null)
        {
            return null;
        }

        // PEReader needs a seekable stream; zip entry streams are not.
        var tempDll = Path.Combine(
            Path.GetTempPath(),
            "sm-" + Guid.NewGuid().ToString("N") + ".dll"
        );
        try
        {
            dllEntry.ExtractToFile(tempDll);
            return AssemblyManifestReader.TryRead(tempDll);
        }
        finally
        {
            try
            {
                File.Delete(tempDll);
            }
            catch (IOException) { }
        }
    }
}
