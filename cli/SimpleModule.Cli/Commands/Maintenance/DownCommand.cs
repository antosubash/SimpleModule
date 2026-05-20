using System.Globalization;
using SimpleModule.Cli.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SimpleModule.Cli.Commands.Maintenance;

public sealed class DownCommand : Command<DownSettings>
{
    public override int Execute(CommandContext context, DownSettings settings)
    {
        var contentRoot = ResolveContentRoot();
        if (contentRoot is null)
        {
            AnsiConsole.MarkupLine(
                "[red]Could not locate the host project. Run `sm down` from within a SimpleModule solution.[/]"
            );
            return 1;
        }

        if (settings.Status)
        {
            return PrintStatus(contentRoot);
        }

        DateTimeOffset? until = null;
        if (!string.IsNullOrWhiteSpace(settings.Until))
        {
            if (
                !DateTimeOffset.TryParse(
                    settings.Until,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var parsed
                )
            )
            {
                AnsiConsole.MarkupLine(
                    $"[red]--until must be a valid ISO-8601 timestamp, got `{settings.Until}`[/]"
                );
                return 1;
            }
            until = parsed;
        }

        if (settings.RetryAfterSeconds < 0)
        {
            AnsiConsole.MarkupLine("[red]--retry must be non-negative.[/]");
            return 1;
        }

        MaintenanceSentinel.Write(
            contentRoot,
            settings.Secret,
            settings.Message,
            settings.RetryAfterSeconds,
            until
        );

        AnsiConsole.MarkupLine(
            $"[green]Maintenance mode enabled.[/] Sentinel: [dim]{MaintenanceSentinel.PathFor(contentRoot)}[/]"
        );

        if (!string.IsNullOrEmpty(settings.Secret))
        {
            AnsiConsole.MarkupLine(
                $"Bypass URL: [cyan]https://<your-host>/?sm_bypass={Markup.Escape(settings.Secret)}[/]"
            );
            AnsiConsole.MarkupLine(
                "[yellow]Keep the secret out of shell history when sharing.[/]"
            );
        }

        return 0;
    }

    private static int PrintStatus(string contentRoot)
    {
        if (!MaintenanceSentinel.Exists(contentRoot))
        {
            AnsiConsole.MarkupLine("[green]Live[/] — no maintenance sentinel present.");
            return 0;
        }

        AnsiConsole.MarkupLine(
            $"[yellow]Maintenance mode active[/]. Sentinel: [dim]{MaintenanceSentinel.PathFor(contentRoot)}[/]"
        );
        return 0;
    }

    private static string? ResolveContentRoot()
    {
        var solution = SolutionContext.Discover();
        if (solution is null)
        {
            return null;
        }
        var hostDir = Path.GetDirectoryName(solution.ApiCsprojPath);
        return string.IsNullOrEmpty(hostDir) ? null : hostDir;
    }
}
