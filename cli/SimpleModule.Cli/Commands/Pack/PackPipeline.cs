using SimpleModule.Cli.Infrastructure;

namespace SimpleModule.Cli.Commands.Pack;

public sealed record ModuleProjectSet(
    string ImplCsproj,
    string? ContractsCsproj,
    string? TestsCsproj
)
{
    public string ImplDirectory => Path.GetDirectoryName(ImplCsproj)!;
    public string AssemblyName => Path.GetFileNameWithoutExtension(ImplCsproj);
}

/// <summary>
/// Pure decision logic behind <c>sm pack</c>: project resolution and manifest
/// validation. Subprocess orchestration lives in <see cref="PackCommand"/>.
/// </summary>
public static class PackPipeline
{
    /// <summary>
    /// MSBuild targets injected via -p:CustomAfterMicrosoftCommonTargets so every
    /// packed module — in-repo or downstream — ships module-manifest.json, is
    /// packable, and carries the simplemodule-module tag, without editing the
    /// user's project files.
    /// </summary>
    public const string PackTargetsContent = """
        <Project>
          <PropertyGroup>
            <IsPackable>true</IsPackable>
            <PackageTags Condition="!$(PackageTags.Contains('simplemodule-module'))">$(PackageTags);simplemodule-module</PackageTags>
          </PropertyGroup>
          <ItemGroup Condition="Exists('$(MSBuildProjectDirectory)/module-manifest.json')">
            <!-- The Web SDK auto-globs *.json as Content, which would flow the manifest
                 into consuming hosts via content/contentFiles. Root-of-package only. -->
            <Content Remove="$(MSBuildProjectDirectory)/module-manifest.json" />
            <None Include="$(MSBuildProjectDirectory)/module-manifest.json"
                  Pack="true"
                  PackagePath="\" />
          </ItemGroup>
        </Project>
        """;

    /// <summary>
    /// Locates the implementation, contracts and test projects under a module
    /// directory (works for both <c>modules/X</c> roots and the impl project dir
    /// itself). Errors are returned, not thrown — pack fails closed with them.
    /// </summary>
    public static (ModuleProjectSet? Projects, string? Error) ResolveModuleProjects(
        string moduleDirectory
    )
    {
        if (!Directory.Exists(moduleDirectory))
        {
            return (null, $"Module directory '{moduleDirectory}' does not exist.");
        }

        var csprojs = Directory
            .EnumerateFiles(moduleDirectory, "*.csproj", SearchOption.AllDirectories)
            .Where(p =>
                !p.Contains(
                    $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
                    StringComparison.Ordinal
                )
            )
            .ToList();

        if (csprojs.Count == 0)
        {
            return (null, $"No .csproj files found under '{moduleDirectory}'.");
        }

        var contracts = csprojs
            .Where(p =>
                Path.GetFileNameWithoutExtension(p).EndsWith(".Contracts", StringComparison.Ordinal)
            )
            .ToList();
        var tests = csprojs
            .Where(p =>
                Path.GetFileNameWithoutExtension(p).EndsWith(".Tests", StringComparison.Ordinal)
            )
            .ToList();
        var impls = csprojs.Except(contracts).Except(tests).ToList();

        if (impls.Count == 0)
        {
            return (
                null,
                $"No module implementation project found under '{moduleDirectory}' "
                    + "(only Contracts/Tests projects present)."
            );
        }

        if (impls.Count > 1)
        {
            return (
                null,
                $"Found {impls.Count} candidate implementation projects under '{moduleDirectory}': "
                    + string.Join(", ", impls.Select(Path.GetFileName))
                    + ". Point sm pack at a single module directory."
            );
        }

        return (
            new ModuleProjectSet(impls[0], contracts.FirstOrDefault(), tests.FirstOrDefault()),
            null
        );
    }

    /// <summary>Validates the built assembly's manifest before packing.</summary>
    public static IReadOnlyList<string> ValidateManifest(
        ModuleManifestData? manifest,
        string expectedAssemblyName,
        string wwwrootDirectory
    )
    {
        var errors = new List<string>();
        if (manifest is null)
        {
            errors.Add(
                "The built assembly carries no module manifest. Ensure the project builds with "
                    + "SimpleModuleProjectKind=Module and references the SimpleModule.Generator analyzer."
            );
            return errors;
        }

        if (manifest.SchemaVersion != 1)
        {
            errors.Add(
                $"Manifest schemaVersion {manifest.SchemaVersion} is not supported by this CLI (expected 1)."
            );
        }

        if (!string.Equals(manifest.Id, expectedAssemblyName, StringComparison.Ordinal))
        {
            errors.Add(
                $"Manifest id '{manifest.Id}' does not match the assembly name '{expectedAssemblyName}'."
            );
        }

        if (string.IsNullOrEmpty(manifest.Name))
        {
            errors.Add("Manifest has no module name — is the [Module] attribute present?");
        }

        if (!string.IsNullOrEmpty(manifest.FrontendEntry))
        {
            var bundleFileName = manifest.FrontendEntry.Split('/').Last();
            var bundlePath = Path.Combine(wwwrootDirectory, bundleFileName);
            if (!File.Exists(bundlePath))
            {
                errors.Add(
                    $"Manifest declares frontend entry '{manifest.FrontendEntry}' but "
                        + $"'{bundlePath}' does not exist. Run the module's Vite build."
                );
            }
        }

        return errors;
    }
}
