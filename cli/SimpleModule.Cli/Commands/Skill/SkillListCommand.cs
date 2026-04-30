using SimpleModule.Cli.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SimpleModule.Cli.Commands.Skill;

public sealed class SkillListCommand : Command<SkillListSettings>
{
    public override int Execute(CommandContext context, SkillListSettings settings)
    {
        var solution = SolutionContext.Discover();
        if (solution is null)
        {
            AnsiConsole.MarkupLine(
                "[red]No .slnx file found. Run this command from inside a SimpleModule project.[/]"
            );
            return 1;
        }

        var lockFile = SkillsLockFile.Load(solution.RootPath);
        var skillsRoot = SkillWriter.GetSkillsRoot(solution.RootPath);

        var onDisk = Directory.Exists(skillsRoot)
            ? Directory
                .GetDirectories(skillsRoot)
                .Select(Path.GetFileName)
                .Where(n => !string.IsNullOrEmpty(n))
                .Cast<string>()
                .ToHashSet(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var allNames = lockFile
            .Skills.Keys.Concat(onDisk)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (allNames.Count == 0)
        {
            AnsiConsole.MarkupLine(
                "[yellow]No skills found.[/] Add one with [green]sm skill add <name> --source <owner/repo>[/]."
            );
            return 0;
        }

        var table = new Table().RoundedBorder();
        table.AddColumn("Skill");
        table.AddColumn("Source");
        table.AddColumn("Ref");
        table.AddColumn("Hash");
        table.AddColumn("Status");

        foreach (var name in allNames)
        {
            lockFile.Skills.TryGetValue(name, out var entry);
            var hasFolder = onDisk.Contains(name);
            var status = (entry, hasFolder) switch
            {
                (not null, true) => "[green]tracked[/]",
                (not null, false) => "[red]missing[/]",
                (null, true) => "[yellow]untracked[/]",
                _ => "—",
            };

            table.AddRow(
                $"[green]{Markup.Escape(name)}[/]",
                Markup.Escape(entry?.Source ?? "—"),
                Markup.Escape(entry?.Ref ?? "—"),
                Markup.Escape(
                    string.IsNullOrEmpty(entry?.ComputedHash) ? "—" : entry.ComputedHash[..8]
                ),
                status
            );
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine(
            $"\n[dim]{allNames.Count} skill(s) in {Markup.Escape(Path.GetRelativePath(solution.RootPath, skillsRoot))}[/]"
        );
        return 0;
    }
}
