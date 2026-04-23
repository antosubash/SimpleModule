using System.Reflection;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SimpleModule.Cli.Commands.Version;

public sealed class VersionCommand : Command
{
    public override int Execute(CommandContext context)
    {
        AnsiConsole.MarkupLine($"[green]sm[/] [dim]v[/]{ResolveVersion()}");
        return 0;
    }

    internal static string ResolveVersion()
    {
        var assembly = typeof(VersionCommand).Assembly;
        var informational = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(informational))
        {
            // Strip the "+<commit-sha>" suffix MSBuild appends to SourceLink builds.
            var plus = informational.IndexOf('+', StringComparison.Ordinal);
            return plus < 0 ? informational : informational[..plus];
        }

        return assembly.GetName().Version?.ToString() ?? "0.0.0";
    }
}
