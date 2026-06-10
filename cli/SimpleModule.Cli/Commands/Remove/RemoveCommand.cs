using System.ComponentModel;
using SimpleModule.Cli.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SimpleModule.Cli.Commands.Remove;

public sealed class RemoveSettings : CommandSettings
{
    [CommandArgument(0, "<package-id>")]
    [Description("NuGet package id of the installed module to remove.")]
    public string PackageId { get; init; } = "";
}

/// <summary>
/// Removes a packaged module's reference from the host (csproj + CPM entry).
/// The module's database schema and data are deliberately left untouched — the
/// command warns loudly about what stays behind instead of dropping anything.
/// </summary>
public sealed class RemoveCommand : Command<RemoveSettings>
{
    public override int Execute(CommandContext context, RemoveSettings settings)
    {
        var solution = SolutionContext.Discover();
        if (solution is null)
        {
            AnsiConsole.MarkupLine(
                "[red]No .slnx file found. Run this command from inside a SimpleModule project.[/]"
            );
            return 1;
        }

        // Resolve the installed version + manifest BEFORE removing the reference
        // so the schema warning can name what is left behind.
        var installed = PackageReferenceManipulator
            .GetPackageReferences(solution.ApiCsprojPath, solution.RootPath)
            .FirstOrDefault(r =>
                string.Equals(r.Id, settings.PackageId, StringComparison.OrdinalIgnoreCase)
            );
        var manifest = GlobalPackagesCache.TryReadManifest(settings.PackageId, installed.Version);

        var removed = PackageReferenceManipulator.RemovePackage(
            solution.ApiCsprojPath,
            solution.RootPath,
            settings.PackageId
        );
        if (!removed)
        {
            AnsiConsole.MarkupLine(
                $"[red]{Markup.Escape(settings.PackageId)} is not referenced by {Markup.Escape(Path.GetFileName(solution.ApiCsprojPath))}.[/]"
            );
            return 1;
        }

        AnsiConsole.MarkupLine(
            $"[green]✓ removed {Markup.Escape(settings.PackageId)} from {Markup.Escape(Path.GetFileName(solution.ApiCsprojPath))}[/]"
        );

        var schemaName = manifest?.Schema ?? "(unknown — manifest unavailable)";
        var warning = new List<string>
        {
            $"Database schema [bold]{Markup.Escape(schemaName)}[/] and all of its tables/data were [bold]left in place[/].",
            "Removing a module never drops data. To clean up manually:",
            $"  · drop the module's tables (schema/prefix: {Markup.Escape(schemaName)})",
            "  · remove its rows from the __EFMigrationsHistory table (if it shipped migrations)",
        };
        if (manifest is not null && manifest.Permissions.Count > 0)
        {
            warning.Add(
                $"  · {manifest.Permissions.Count} permission(s) granted to roles remain until re-saved"
            );
        }

        AnsiConsole.Write(
            new Panel(string.Join("\n", warning))
                .Header("[yellow bold]Left behind[/]")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Yellow)
        );
        AnsiConsole.MarkupLine("[dim]Run 'dotnet build' to verify the host still compiles.[/]");

        return 0;
    }
}
