using System.ComponentModel;
using SimpleModule.Cli.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SimpleModule.Cli.Commands.Upgrade;

public sealed class UpgradeSettings : CommandSettings
{
    [CommandArgument(0, "[package-id]")]
    [Description("Module package to upgrade. Omit to upgrade every installed module.")]
    public string? PackageId { get; init; }

    [CommandOption("--version")]
    [Description("Target version. Default: highest stable available.")]
    public string? Version { get; init; }

    [CommandOption("--source")]
    [Description(
        "Package source (local folder feed or NuGet V3 service index URL). Default: sm.json registry."
    )]
    public string? Source { get; init; }

    [CommandOption("--force")]
    [Description(
        "Upgrade even when the target version's framework compat range rejects this host."
    )]
    public bool Force { get; init; }

    [CommandOption("--skip-migrations")]
    [Description("Do not run the host in migrate-only mode after upgrading.")]
    public bool SkipMigrations { get; init; }
}

/// <summary>
/// Upgrades installed module packages: resolves the target version, validates
/// the new manifest's framework compat range (refusing violations unless
/// --force), bumps the (CPM-aware) reference, rebuilds and applies migrations.
/// </summary>
public sealed class UpgradeCommand : AsyncCommand<UpgradeSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, UpgradeSettings settings)
    {
        var solution = SolutionContext.Discover();
        if (solution is null)
        {
            return Fail(
                "No .slnx file found. Run this command from inside a SimpleModule project."
            );
        }

        // Installed module packages = references whose cached package carries a manifest.
        var installed = PackageReferenceManipulator
            .GetPackageReferences(solution.ApiCsprojPath, solution.RootPath)
            .Where(r =>
                settings.PackageId is null
                    ? GlobalPackagesCache.TryReadManifest(r.Id, r.Version) is not null
                    : string.Equals(r.Id, settings.PackageId, StringComparison.OrdinalIgnoreCase)
            )
            .ToList();

        if (settings.Version is not null && settings.PackageId is null)
        {
            return Fail("--version requires a package id (versions are per-package).");
        }

        if (installed.Count == 0)
        {
            return Fail(
                settings.PackageId is null
                    ? "No installed module packages found."
                    : $"{settings.PackageId} is not referenced by {Path.GetFileName(solution.ApiCsprojPath)}."
            );
        }

        var source = settings.Source ?? SmConfig.Load(solution.RootPath).Registry;
        if (NuGetClient.IsLocalDirectorySource(source))
        {
            // The bumped reference must be restorable: register local feeds just
            // like sm add does.
            NuGetConfigManipulator.EnsureLocalSource(solution.RootPath, Path.GetFullPath(source));
        }

        var hostVersion = HostFrameworkVersionResolver.Resolve(solution.RootPath);
        var upgraded = 0;
        var anyHasDbContext = false;

        foreach (var reference in installed)
        {
            var (nupkgPath, targetVersion, error) = await ResolveTarget(
                source,
                reference.Id,
                settings.Version
            );
            if (nupkgPath is null)
            {
                return Fail(error!);
            }

            if (targetVersion == reference.Version)
            {
                AnsiConsole.MarkupLine(
                    $"[dim]{Markup.Escape(reference.Id)} already at {Markup.Escape(targetVersion)} — skipping[/]"
                );
                continue;
            }

            var manifest = NupkgManifestReader.TryRead(nupkgPath, reference.Id);
            if (nupkgPath.Contains("sm-upgrade-", StringComparison.Ordinal))
            {
                try
                {
                    File.Delete(nupkgPath);
                }
                catch (IOException) { }
            }

            if (manifest is null)
            {
                return Fail(
                    $"{reference.Id} {targetVersion} carries no module manifest — refusing to upgrade."
                );
            }

            if (hostVersion is not null)
            {
                var compat = FrameworkCompatChecker.Check(manifest.FrameworkCompat, hostVersion);
                if (!compat.Compatible && !settings.Force)
                {
                    return Fail(
                        $"Refusing to upgrade {reference.Id} {reference.Version} → {targetVersion}: "
                            + $"{compat.Reason} (use --force to override)"
                    );
                }

                if (!compat.Compatible)
                {
                    AnsiConsole.MarkupLine(
                        $"[yellow]! --force: upgrading despite incompatibility — {Markup.Escape(compat.Reason)}[/]"
                    );
                }
            }

            PackageReferenceManipulator.AddPackage(
                solution.ApiCsprojPath,
                solution.RootPath,
                reference.Id,
                targetVersion
            );
            AnsiConsole.MarkupLine(
                $"[green]✓ {Markup.Escape(reference.Id)} {Markup.Escape(reference.Version ?? "?")} → {Markup.Escape(targetVersion)}[/]"
            );
            upgraded++;
            anyHasDbContext |= manifest.HasDbContext;
        }

        if (upgraded == 0)
        {
            AnsiConsole.MarkupLine("[green]Everything is up to date.[/]");
            return 0;
        }

        AnsiConsole.MarkupLine("[dim]→ dotnet build...[/]");
        var build = await ProcessRunner.RunAsync(
            "dotnet",
            ["build", solution.ApiCsprojPath],
            solution.RootPath
        );
        if (!build.Success)
        {
            return Fail(
                "Build failed after the upgrade — references were left in place for inspection:\n"
                    + Tail(build.Error, build.Output)
            );
        }

        if (!settings.SkipMigrations && anyHasDbContext)
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
                    "Module migration run failed (fix the database issue and re-run "
                        + "'SIMPLEMODULE_MIGRATE_ONLY=1 dotnet run --project <host>'):\n"
                        + Tail(migrate.Error, migrate.Output)
                );
            }

            AnsiConsole.MarkupLine("[green]✓ database migrated[/]");
        }

        AnsiConsole.MarkupLine($"[green]✓ upgraded {upgraded} module(s)[/]");
        return 0;
    }

    private static async Task<(string? NupkgPath, string Version, string? Error)> ResolveTarget(
        string source,
        string packageId,
        string? requestedVersion
    )
    {
        if (NuGetClient.IsLocalDirectorySource(source))
        {
            var feedDir = Path.GetFullPath(source);
            var path = NuGetClient.FindLocalNupkg(feedDir, packageId, requestedVersion);
            return path is null
                ? (null, "", $"Package {packageId} not found in local feed '{feedDir}'.")
                : (path, Path.GetFileNameWithoutExtension(path)[(packageId.Length + 1)..], null);
        }

        var serviceIndex = new Uri(source);
        var versions = await NuGetClient.GetVersionsAsync(serviceIndex, packageId);
        if (versions.Count == 0)
        {
            return (null, "", $"Package {packageId} not found on '{source}'.");
        }

        var sorted = versions.OrderBy(v => v, SemVerStringComparer.Instance).ToList();
        var target =
            requestedVersion
            ?? sorted.LastOrDefault(v => !SemVerStringComparer.IsPrerelease(v))
            ?? sorted[^1];
        if (!versions.Contains(target))
        {
            return (null, "", $"Version {target} of {packageId} not found on '{source}'.");
        }

        var tempPath = Path.Combine(
            Path.GetTempPath(),
            $"sm-upgrade-{Guid.NewGuid():N}-{packageId}.{target}.nupkg"
        );
        await NuGetClient.DownloadNupkgAsync(serviceIndex, packageId, target, tempPath);
        return (tempPath, target, null);
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
