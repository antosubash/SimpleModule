using System.ComponentModel;
using SimpleModule.Cli.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SimpleModule.Cli.Commands.Pack;

public sealed class PackSettings : CommandSettings
{
    [CommandArgument(0, "[module-path]")]
    [Description("Path to the module directory (defaults to the current directory).")]
    public string? ModulePath { get; init; }

    [CommandOption("-o|--output")]
    [Description("Output directory for the nupkg(s). Default: ./artifacts/packages")]
    public string? Output { get; init; }

    [CommandOption("--version")]
    [Description("Package version override (passed as -p:Version to build and pack).")]
    public string? Version { get; init; }

    [CommandOption("--skip-tests")]
    [Description("Skip running the module's test project.")]
    public bool SkipTests { get; init; }

    [CommandOption("-c|--configuration")]
    [Description("Build configuration. Default: Release")]
    public string Configuration { get; init; } = "Release";
}

/// <summary>
/// Builds, validates and packs a module into a standard nupkg:
/// production frontend build → externals validation → dotnet build → tests →
/// manifest validation → dotnet pack (module + contracts). Fails closed at the
/// first violated step.
/// </summary>
public sealed class PackCommand : AsyncCommand<PackSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, PackSettings settings)
    {
        var moduleDir = Path.GetFullPath(settings.ModulePath ?? ".");
        var (projects, resolveError) = PackPipeline.ResolveModuleProjects(moduleDir);
        if (projects is null)
        {
            return Fail(resolveError!);
        }

        var outputDir = Path.GetFullPath(
            settings.Output
                ?? Path.Combine(
                    SolutionContext.Discover()?.RootPath ?? moduleDir,
                    "artifacts",
                    "packages"
                )
        );
        Directory.CreateDirectory(outputDir);

        AnsiConsole.MarkupLine(
            $"Packing module [green]{Markup.Escape(projects.AssemblyName)}[/] from [blue]{Markup.Escape(moduleDir)}[/]"
        );

        // 1. Production frontend build (when the module has one)
        var implDir = projects.ImplDirectory;
        if (File.Exists(Path.Combine(implDir, "package.json")))
        {
            if (!HasNodeModules(implDir))
            {
                return Fail(
                    "package.json found but no node_modules is available. "
                        + "Run 'npm install' before packing so the frontend bundle can be built."
                );
            }

            AnsiConsole.MarkupLine("[dim]→ building frontend (vite, production)...[/]");
            var vite = await ProcessRunner.RunAsync(
                "npx",
                ["vite", "build", "--configLoader", "runner"],
                implDir
            );
            if (!vite.Success)
            {
                return Fail("Frontend build failed:\n" + Tail(vite.Error, vite.Output));
            }

            // 2. Externals validation — fail closed on inlined React
            var violations = BundleExternalsValidator.Validate(Path.Combine(implDir, "wwwroot"));
            if (violations.Count > 0)
            {
                foreach (var violation in violations)
                {
                    AnsiConsole.MarkupLine(
                        $"[red]✗ {Markup.Escape(Path.GetFileName(violation.File))} inlines a host-provided library (marker: {Markup.Escape(violation.Marker)})[/]"
                    );
                }

                return Fail(
                    "Module bundles must externalize react, react-dom, react/jsx-runtime and "
                        + "@inertiajs/react — they are provided by the host. Check the module's "
                        + "vite.config.ts uses defineModuleConfig from @simplemodule/client."
                );
            }
        }

        var versionProps = settings.Version is null
            ? Array.Empty<string>()
            : [$"-p:Version={settings.Version}"];

        // 3. Backend build
        AnsiConsole.MarkupLine("[dim]→ dotnet build...[/]");
        var build = await ProcessRunner.RunAsync(
            "dotnet",
            ["build", projects.ImplCsproj, "-c", settings.Configuration, .. versionProps],
            implDir
        );
        if (!build.Success)
        {
            return Fail("dotnet build failed:\n" + Tail(build.Error, build.Output));
        }

        // 4. Tests
        if (!settings.SkipTests && projects.TestsCsproj is not null)
        {
            AnsiConsole.MarkupLine("[dim]→ dotnet test...[/]");
            var test = await ProcessRunner.RunAsync(
                "dotnet",
                ["test", projects.TestsCsproj, "-c", settings.Configuration, .. versionProps],
                implDir
            );
            if (!test.Success)
            {
                return Fail("Module tests failed:\n" + Tail(test.Error, test.Output));
            }
        }
        else if (!settings.SkipTests)
        {
            AnsiConsole.MarkupLine("[yellow]! no test project found — skipping tests[/]");
        }

        // 5. Manifest validation from the built assembly
        var builtDll = FindBuiltAssembly(implDir, settings.Configuration, projects.AssemblyName);
        if (builtDll is null)
        {
            return Fail(
                $"Could not locate the built assembly {projects.AssemblyName}.dll under bin/{settings.Configuration}."
            );
        }

        var manifest = AssemblyManifestReader.TryRead(builtDll);
        var manifestErrors = PackPipeline.ValidateManifest(
            manifest,
            projects.AssemblyName,
            Path.Combine(implDir, "wwwroot")
        );
        if (manifestErrors.Count > 0)
        {
            foreach (var error in manifestErrors)
            {
                AnsiConsole.MarkupLine($"[red]✗ {Markup.Escape(error)}[/]");
            }

            return Fail("Module manifest validation failed.");
        }

        // 6. Write module-manifest.json next to the project so the injected
        // targets pack it into the nupkg root for registry/tooling consumption.
        var manifestJson = AssemblyManifestReader.TryReadJson(builtDll)!;
        await File.WriteAllTextAsync(Path.Combine(implDir, "module-manifest.json"), manifestJson);

        var targetsPath = Path.Combine(
            Path.GetTempPath(),
            "sm-pack-" + Guid.NewGuid().ToString("N") + ".targets"
        );
        await File.WriteAllTextAsync(targetsPath, PackPipeline.PackTargetsContent);

        try
        {
            // 7. Pack module (+ contracts)
            var packTargets = new List<string> { projects.ImplCsproj };
            if (projects.ContractsCsproj is not null)
            {
                packTargets.Add(projects.ContractsCsproj);
            }

            foreach (var csproj in packTargets)
            {
                AnsiConsole.MarkupLine(
                    $"[dim]→ dotnet pack {Markup.Escape(Path.GetFileName(csproj))}...[/]"
                );
                var pack = await ProcessRunner.RunAsync(
                    "dotnet",
                    [
                        "pack",
                        csproj,
                        "-c",
                        settings.Configuration,
                        "-o",
                        outputDir,
                        $"-p:CustomAfterMicrosoftCommonTargets={targetsPath}",
                        .. versionProps,
                    ],
                    implDir
                );
                if (!pack.Success)
                {
                    return Fail("dotnet pack failed:\n" + Tail(pack.Error, pack.Output));
                }
            }
        }
        finally
        {
            try
            {
                File.Delete(targetsPath);
            }
            catch (IOException) { }
        }

        AnsiConsole.MarkupLine(
            $"[green]✓ Packed {Markup.Escape(manifest!.DisplayName)} {Markup.Escape(manifest.Version)}[/] → [blue]{Markup.Escape(outputDir)}[/]"
        );
        AnsiConsole.MarkupLine(
            $"[dim]  schema: {Markup.Escape(manifest.Schema)}  permissions: {manifest.Permissions.Count}  pages: {manifest.Pages.Count}  frameworkCompat: {Markup.Escape(manifest.FrameworkCompat)}[/]"
        );

        return 0;
    }

    private static bool HasNodeModules(string implDir)
    {
        var dir = new DirectoryInfo(implDir);
        while (dir is not null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "node_modules")))
            {
                return true;
            }

            dir = dir.Parent;
        }

        return false;
    }

    private static string? FindBuiltAssembly(
        string implDir,
        string configuration,
        string assemblyName
    )
    {
        var binDir = Path.Combine(implDir, "bin", configuration);
        if (!Directory.Exists(binDir))
        {
            return null;
        }

        return Directory
            .EnumerateFiles(binDir, assemblyName + ".dll", SearchOption.AllDirectories)
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();
    }

    private static string Tail(string error, string output)
    {
        var text = string.IsNullOrWhiteSpace(error) ? output : error;
        var lines = text.Split('\n');
        return string.Join('\n', lines.TakeLast(25)).Trim();
    }

    private static int Fail(string message)
    {
        AnsiConsole.MarkupLine($"[red]{Markup.Escape(message)}[/]");
        return 1;
    }
}
