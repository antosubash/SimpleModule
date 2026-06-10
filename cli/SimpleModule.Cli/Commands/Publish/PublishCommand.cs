using System.ComponentModel;
using SimpleModule.Cli.Commands.Pack;
using SimpleModule.Cli.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SimpleModule.Cli.Commands.Publish;

public sealed class PublishSettings : CommandSettings
{
    [CommandArgument(0, "[module-path]")]
    [Description("Path to the module directory (defaults to the current directory).")]
    public string? ModulePath { get; init; }

    [CommandOption("--version")]
    [Description("Package version (passed to the pack step).")]
    public string? Version { get; init; }

    [CommandOption("--source")]
    [Description(
        "Push target: a NuGet source URL or a local folder feed. Default: the registry from sm.json (nuget.org)."
    )]
    public string? Source { get; init; }

    [CommandOption("--api-key")]
    [Description("NuGet API key. Falls back to the NUGET_API_KEY environment variable.")]
    public string? ApiKey { get; init; }

    [CommandOption("--dry-run")]
    [Description(
        "Run the full pack + validation pipeline and show what would be pushed, without pushing."
    )]
    public bool DryRun { get; init; }

    [CommandOption("--skip-tests")]
    [Description("Skip running the module's test project during pack.")]
    public bool SkipTests { get; init; }

    [CommandOption("--register")]
    [Description("Register the package with the SimpleModule marketplace (not available yet).")]
    public bool Register { get; init; }
}

/// <summary>
/// Packs a module (full sm pack pipeline) and pushes the resulting nupkgs via
/// `dotnet nuget push`. `--dry-run` stops after pack and prints the would-be
/// push; `--register` is a documented extension point for the future
/// marketplace and currently only explains that it is not available.
/// </summary>
public sealed class PublishCommand : AsyncCommand<PublishSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, PublishSettings settings)
    {
        var outputDir = Path.Combine(
            Path.GetTempPath(),
            "sm-publish-" + Guid.NewGuid().ToString("N")
        );

        var (packExit, packages) = await PackCommand.RunAsync(
            new PackSettings
            {
                ModulePath = settings.ModulePath,
                Version = settings.Version,
                SkipTests = settings.SkipTests,
                Output = outputDir,
            }
        );
        if (packExit != 0)
        {
            return packExit;
        }

        try
        {
            var source = settings.Source ?? SmConfig.Load(Directory.GetCurrentDirectory()).Registry;
            var apiKey = settings.ApiKey ?? Environment.GetEnvironmentVariable("NUGET_API_KEY");

            if (settings.DryRun)
            {
                AnsiConsole.MarkupLine(
                    $"[yellow]--dry-run:[/] would push to [blue]{Markup.Escape(source)}[/]"
                        + (
                            apiKey is null
                                ? " [dim](no API key configured)[/]"
                                : " [dim](API key configured)[/]"
                        )
                );
                foreach (var package in packages)
                {
                    AnsiConsole.MarkupLine($"[dim]  {Markup.Escape(Path.GetFileName(package))}[/]");
                }

                return 0;
            }

            var isLocal = NuGetClient.IsLocalDirectorySource(source);
            if (!isLocal && string.IsNullOrEmpty(apiKey))
            {
                return Fail(
                    "No API key. Pass --api-key or set the NUGET_API_KEY environment variable."
                );
            }

            if (isLocal)
            {
                Directory.CreateDirectory(source);
            }

            foreach (var package in packages)
            {
                AnsiConsole.MarkupLine(
                    $"[dim]→ pushing {Markup.Escape(Path.GetFileName(package))} to {Markup.Escape(source)}...[/]"
                );
                var pushArgs = new List<string> { "nuget", "push", package, "--source", source };
                if (!isLocal)
                {
                    pushArgs.AddRange(["--api-key", apiKey!]);
                }

                var push = await ProcessRunner.RunAsync("dotnet", pushArgs);
                if (!push.Success)
                {
                    return Fail(
                        "dotnet nuget push failed:\n"
                            + (
                                string.IsNullOrWhiteSpace(push.Error) ? push.Output : push.Error
                            ).Trim()
                    );
                }
            }

            AnsiConsole.MarkupLine(
                $"[green]✓ published {packages.Count} package(s) to {Markup.Escape(source)}[/]"
            );

            if (settings.Register)
            {
                AnsiConsole.MarkupLine(
                    "[yellow]--register: marketplace registration is not available yet.[/] "
                        + "The package is already discoverable through the registry via the "
                        + "[bold]simplemodule-module[/] tag (sm search). This flag will register "
                        + "the module with the SimpleModule marketplace once it launches."
                );
            }

            return 0;
        }
        finally
        {
            try
            {
                Directory.Delete(outputDir, recursive: true);
            }
            catch (IOException) { }
        }
    }

    private static int Fail(string message)
    {
        AnsiConsole.MarkupLine($"[red]{Markup.Escape(message)}[/]");
        return 1;
    }
}
