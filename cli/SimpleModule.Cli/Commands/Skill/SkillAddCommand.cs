using SimpleModule.Cli.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SimpleModule.Cli.Commands.Skill;

public sealed class SkillAddCommand : AsyncCommand<SkillAddSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, SkillAddSettings settings)
    {
        var solution = SolutionContext.Discover();
        if (solution is null)
        {
            AnsiConsole.MarkupLine(
                "[red]No .slnx file found. Run this command from inside a SimpleModule project.[/]"
            );
            return 1;
        }

        var name = settings.ResolveName();
        var skillDir = SkillWriter.GetSkillDirectory(solution.RootPath, name);

        if (Directory.Exists(skillDir) && !settings.Force && !settings.DryRun)
        {
            AnsiConsole.MarkupLine(
                $"[red]Skill '{Markup.Escape(name)}' already exists at {Markup.Escape(Path.GetRelativePath(solution.RootPath, skillDir))}. Use --force to overwrite or 'sm skill update' to refresh.[/]"
            );
            return 1;
        }

        SkillSource source;
        FetchedSkill fetched;

        if (string.IsNullOrWhiteSpace(settings.Source))
        {
            source = new SkillSource(SkillSourceType.Scaffold, "scaffold");
            fetched = SkillWriter.BuildScaffold(name, settings.Description);
            AnsiConsole.MarkupLine($"[blue]Scaffolding new skill '{Markup.Escape(name)}'.[/]");
        }
        else
        {
            source = SkillSource.Parse(settings.Source!);
            AnsiConsole.MarkupLine(
                $"[blue]Fetching skill '{Markup.Escape(name)}' from {Markup.Escape(source.Type.ToString().ToLowerInvariant())} source '{Markup.Escape(source.CanonicalSource)}'...[/]"
            );

            try
            {
                fetched = await new SkillFetcher().FetchAsync(source).ConfigureAwait(false);
            }
            catch (InvalidOperationException ex)
            {
                AnsiConsole.MarkupLine(
                    $"[red]Failed to fetch skill: {Markup.Escape(ex.Message)}[/]"
                );
                return 1;
            }
        }

        if (settings.DryRun)
        {
            AnsiConsole.MarkupLine("[yellow]Dry run — no files will be written.[/]");
            foreach (var file in fetched.Files)
            {
                var rel = Path.GetRelativePath(
                    solution.RootPath,
                    Path.Combine(skillDir, file.RelativePath)
                );
                AnsiConsole.MarkupLine($"  [green]CREATE[/] {Markup.Escape(rel)}");
            }

            AnsiConsole.MarkupLine(
                $"  [green]UPDATE[/] {Markup.Escape(SkillsLockFile.FileName)} (entry '{Markup.Escape(name)}')"
            );
            return 0;
        }

        var written = SkillWriter.WriteFiles(skillDir, fetched.Files, replace: settings.Force);
        foreach (var path in written)
        {
            var rel = Path.GetRelativePath(solution.RootPath, Path.Combine(skillDir, path));
            AnsiConsole.MarkupLine($"  [green]CREATE[/] {Markup.Escape(rel)}");
        }

        var lockFile = SkillsLockFile.Load(solution.RootPath);
        lockFile.Skills[name] = new SkillsLockEntry
        {
            Source = source.CanonicalSource,
            SourceType = source.SourceTypeId,
            Ref = source.Ref,
            ComputedHash = fetched.ComputedHash,
        };
        lockFile.Save(solution.RootPath);

        AnsiConsole.MarkupLine($"  [green]UPDATE[/] {Markup.Escape(SkillsLockFile.FileName)}");
        AnsiConsole.MarkupLine(
            $"\n[green]Skill '{Markup.Escape(name)}' added.[/] [dim]({fetched.Files.Count} file(s), hash {fetched.ComputedHash[..8]})[/]"
        );
        return 0;
    }
}
