using System.ComponentModel;
using SimpleModule.Cli.Commands.Doctor;
using SimpleModule.Cli.Commands.Doctor.Checks;
using SimpleModule.Cli.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SimpleModule.Cli.Commands.Add;

public sealed class AddSettings : CommandSettings
{
    [CommandArgument(0, "<package-id>")]
    [Description("NuGet package id of the module (e.g. SimpleModule.Products).")]
    public string PackageId { get; init; } = "";

    [CommandOption("--version")]
    [Description("Exact package version. Default: highest available.")]
    public string? Version { get; init; }

    [CommandOption("--source")]
    [Description(
        "Package source: a local folder feed or a NuGet V3 service index URL. Default: the registry from sm.json (nuget.org)."
    )]
    public string? Source { get; init; }

    [CommandOption("--skip-migrations")]
    [Description("Do not run the host in migrate-only mode after install.")]
    public bool SkipMigrations { get; init; }

    [CommandOption("--skip-doctor")]
    [Description("Do not run sm doctor after install.")]
    public bool SkipDoctor { get; init; }
}

/// <summary>
/// Installs a packaged module into the host: resolves the nupkg, validates the
/// module manifest and framework compatibility BEFORE touching any file, wires
/// the (CPM-aware) package reference, restores + builds, applies module
/// migrations via the SIMPLEMODULE_MIGRATE_ONLY hook, then runs doctor.
/// </summary>
public sealed class AddCommand : AsyncCommand<AddSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, AddSettings settings)
    {
        var solution = SolutionContext.Discover();
        if (solution is null)
        {
            return Fail(
                "No .slnx file found. Run this command from inside a SimpleModule project."
            );
        }

        // 1. Resolve the package source and obtain the nupkg
        var source = settings.Source ?? SmConfig.Load(solution.RootPath).Registry;
        var isLocalSource = NuGetClient.IsLocalDirectorySource(source);

        string? nupkgPath;
        string resolvedVersion;
        if (isLocalSource)
        {
            var feedDir = Path.GetFullPath(source);
            nupkgPath = NuGetClient.FindLocalNupkg(feedDir, settings.PackageId, settings.Version);
            if (nupkgPath is null)
            {
                return Fail(
                    $"Package {settings.PackageId}"
                        + (settings.Version is null ? "" : $" {settings.Version}")
                        + $" not found in local feed '{feedDir}'."
                );
            }

            resolvedVersion = Path.GetFileNameWithoutExtension(nupkgPath)[
                (settings.PackageId.Length + 1)..
            ];
        }
        else
        {
            var serviceIndex = new Uri(source);
            var versions = await NuGetClient.GetVersionsAsync(serviceIndex, settings.PackageId);
            if (versions.Count == 0)
            {
                return Fail($"Package {settings.PackageId} not found on '{source}'.");
            }

            // "Latest" prefers the highest stable version; prereleases only when
            // nothing stable exists. The flat-container array's order is not
            // guaranteed by every feed, so sort explicitly.
            var sorted = versions.OrderBy(v => v, SemVerStringComparer.Instance).ToList();
            resolvedVersion =
                settings.Version
                ?? sorted.LastOrDefault(v => !SemVerStringComparer.IsPrerelease(v))
                ?? sorted[^1];
            if (!versions.Contains(resolvedVersion))
            {
                return Fail(
                    $"Version {resolvedVersion} of {settings.PackageId} not found on '{source}'. "
                        + $"Available: {string.Join(", ", versions.TakeLast(8))}"
                );
            }

            nupkgPath = Path.Combine(
                Path.GetTempPath(),
                $"{settings.PackageId}.{resolvedVersion}.nupkg"
            );
            AnsiConsole.MarkupLine(
                $"[dim]→ downloading {Markup.Escape(settings.PackageId)} {Markup.Escape(resolvedVersion)}...[/]"
            );
            await NuGetClient.DownloadNupkgAsync(
                serviceIndex,
                settings.PackageId,
                resolvedVersion,
                nupkgPath
            );
        }

        // 2. Manifest — required for module packages
        var manifest = NupkgManifestReader.TryRead(nupkgPath, settings.PackageId);
        if (manifest is null)
        {
            return Fail(
                $"{settings.PackageId} carries no SimpleModule module manifest — it is not a "
                    + "SimpleModule module package. Use 'sm install' for plain NuGet packages."
            );
        }

        // 3. Framework compatibility gate BEFORE any file change
        var hostVersion = HostFrameworkVersionResolver.Resolve(solution.RootPath);
        if (hostVersion is null)
        {
            AnsiConsole.MarkupLine(
                "[yellow]! could not determine the host's SimpleModule.Core version — skipping compat check[/]"
            );
        }
        else
        {
            var compat = FrameworkCompatChecker.Check(manifest.FrameworkCompat, hostVersion);
            if (!compat.Compatible)
            {
                return Fail(
                    $"Refusing to install {settings.PackageId} {resolvedVersion}: {compat.Reason}"
                );
            }

            AnsiConsole.MarkupLine($"[dim]✓ {Markup.Escape(compat.Reason)}[/]");
        }

        // 4. Local feeds must be resolvable at restore time
        if (isLocalSource)
        {
            NuGetConfigManipulator.EnsureLocalSource(solution.RootPath, Path.GetFullPath(source));
        }

        // 5. Wire the package reference (CPM-aware)
        PackageReferenceManipulator.AddPackage(
            solution.ApiCsprojPath,
            solution.RootPath,
            settings.PackageId,
            resolvedVersion
        );
        AnsiConsole.MarkupLine(
            $"[green]✓ added {Markup.Escape(settings.PackageId)} {Markup.Escape(resolvedVersion)} to {Markup.Escape(Path.GetFileName(solution.ApiCsprojPath))}[/]"
        );

        // 6. Restore + build
        AnsiConsole.MarkupLine("[dim]→ dotnet restore + build...[/]");
        var build = await ProcessRunner.RunAsync(
            "dotnet",
            ["build", solution.ApiCsprojPath],
            solution.RootPath
        );
        if (!build.Success)
        {
            return Fail(
                "Build failed after adding the package — the reference was left in place for inspection:\n"
                    + Tail(build.Error, build.Output)
            );
        }

        // 7. Apply module migrations deterministically
        if (!settings.SkipMigrations && manifest.HasDbContext)
        {
            AnsiConsole.MarkupLine("[dim]→ applying module migrations (migrate-only run)...[/]");
            var migrate = await ProcessRunner.RunAsync(
                "dotnet",
                ["run", "--project", Path.GetDirectoryName(solution.ApiCsprojPath)!, "--no-build"],
                solution.RootPath,
                new Dictionary<string, string> { ["SIMPLEMODULE_MIGRATE_ONLY"] = "1" }
            );
            if (!migrate.Success)
            {
                return Fail(
                    "Module migration run failed (the package reference is in place; fix the "
                        + "database issue and re-run 'SIMPLEMODULE_MIGRATE_ONLY=1 dotnet run --project <host>'):\n"
                        + Tail(migrate.Error, migrate.Output)
                );
            }

            AnsiConsole.MarkupLine("[green]✓ database initialized[/]");
        }

        PrintSummary(manifest, resolvedVersion);

        // 8. Doctor
        if (!settings.SkipDoctor)
        {
            AnsiConsole.Write(new Rule("[blue]sm doctor[/]").LeftJustified());
            var results = DoctorCommand.RunChecks(solution);
            DoctorCommand.RenderResults(results);
            if (results.Exists(r => r.Status == CheckStatus.Fail))
            {
                AnsiConsole.MarkupLine(
                    "[yellow]! doctor reported failures — run 'sm doctor --fix'[/]"
                );
            }
        }

        return 0;
    }

    private static void PrintSummary(ModuleManifestData manifest, string version)
    {
        AnsiConsole.MarkupLine(
            $"[green]✓ Installed {Markup.Escape(manifest.DisplayName)}[/] [dim]({Markup.Escape(manifest.Id)} {Markup.Escape(version)})[/]"
        );
        AnsiConsole.MarkupLine(
            $"[dim]  schema: {Markup.Escape(manifest.Schema)}  permissions: {manifest.Permissions.Count}"
                + $"  pages: {manifest.Pages.Count}"
                + (
                    manifest.FrontendEntry is null
                        ? "  (backend-only)"
                        : $"  frontend: {Markup.Escape(manifest.FrontendEntry)}"
                )
                + "[/]"
        );
    }

    private static string Tail(string error, string output)
    {
        var text = string.IsNullOrWhiteSpace(error) ? output : error;
        return string.Join('\n', text.Split('\n').TakeLast(25)).Trim();
    }

    private static int Fail(string message)
    {
        AnsiConsole.MarkupLine($"[red]{Markup.Escape(message)}[/]");
        return 1;
    }
}
