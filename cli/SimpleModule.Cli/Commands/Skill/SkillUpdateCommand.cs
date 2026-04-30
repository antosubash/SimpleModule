using SimpleModule.Cli.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SimpleModule.Cli.Commands.Skill;

public sealed class SkillUpdateCommand : AsyncCommand<SkillUpdateSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        SkillUpdateSettings settings
    )
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
        if (lockFile.Skills.Count == 0)
        {
            AnsiConsole.MarkupLine(
                $"[yellow]No skills tracked in {SkillsLockFile.FileName}. Use 'sm skill add' first.[/]"
            );
            return 0;
        }

        IEnumerable<string> targets;
        if (string.IsNullOrWhiteSpace(settings.Name))
        {
            targets = lockFile.Skills.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase);
        }
        else
        {
            var name = SkillNameValidator.Normalize(settings.Name);
            if (!lockFile.Skills.ContainsKey(name))
            {
                AnsiConsole.MarkupLine(
                    $"[red]Skill '{Markup.Escape(name)}' is not tracked in {SkillsLockFile.FileName}.[/]"
                );
                return 1;
            }

            targets = [name];
        }

        var fetcher = new SkillFetcher();

        var changed = 0;
        var unchanged = 0;
        var failed = 0;

        var refOverride = !string.IsNullOrWhiteSpace(settings.Name) ? settings.Ref : null;

        foreach (var name in targets)
        {
            var entry = lockFile.Skills[name];

            if (
                string.Equals(
                    entry.SourceType,
                    SkillSource.TypeIdScaffold,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                AnsiConsole.MarkupLine(
                    $"  [yellow]SKIP[/] {Markup.Escape(name)} (scaffolded; no remote source to update)"
                );
                continue;
            }

            SkillSource source;
            try
            {
                source = SkillSource.Parse(entry.Source);
            }
            catch (ArgumentException ex)
            {
                AnsiConsole.MarkupLine(
                    $"  [red]FAIL[/] {Markup.Escape(name)} — invalid source: {Markup.Escape(ex.Message)}"
                );
                failed++;
                continue;
            }

            // CanonicalSource strips @ref, so source.Ref is null after Parse — restore it
            // from the explicit --ref override (when targeting one skill) or the lock entry.
            if (source.Type == SkillSourceType.GitHub)
            {
                var resolvedRef = !string.IsNullOrWhiteSpace(refOverride) ? refOverride : entry.Ref;
                if (!string.IsNullOrEmpty(resolvedRef))
                {
                    source = source with { Ref = resolvedRef };
                }
            }

            FetchedSkill fetched;
            try
            {
                fetched = await fetcher.FetchAsync(source).ConfigureAwait(false);
            }
            catch (InvalidOperationException ex)
            {
                AnsiConsole.MarkupLine(
                    $"  [red]FAIL[/] {Markup.Escape(name)} — {Markup.Escape(ex.Message)}"
                );
                failed++;
                continue;
            }

            var sameHash = string.Equals(
                fetched.ComputedHash,
                entry.ComputedHash,
                StringComparison.OrdinalIgnoreCase
            );

            if (sameHash)
            {
                AnsiConsole.MarkupLine($"  [dim]OK[/]   {Markup.Escape(name)} (up to date)");
                unchanged++;
                continue;
            }

            if (settings.Check)
            {
                AnsiConsole.MarkupLine(
                    $"  [yellow]DRIFT[/] {Markup.Escape(name)} (recorded {entry.ComputedHash[..8]} → remote {fetched.ComputedHash[..8]})"
                );
                changed++;
                continue;
            }

            if (settings.DryRun)
            {
                AnsiConsole.MarkupLine(
                    $"  [yellow]CHANGE[/] {Markup.Escape(name)} ({fetched.Files.Count} file(s), new hash {fetched.ComputedHash[..8]})"
                );
                changed++;
                continue;
            }

            var skillDir = SkillWriter.GetSkillDirectory(solution.RootPath, name);
            SkillWriter.WriteFiles(skillDir, fetched.Files, replace: true);

            entry.ComputedHash = fetched.ComputedHash;
            entry.Source = source.CanonicalSource;
            entry.SourceType = source.SourceTypeId;
            entry.Ref = source.Ref ?? entry.Ref;

            AnsiConsole.MarkupLine(
                $"  [green]UPDATED[/] {Markup.Escape(name)} ({fetched.Files.Count} file(s), hash {fetched.ComputedHash[..8]})"
            );
            changed++;
        }

        if (!settings.DryRun && !settings.Check && changed > 0)
        {
            lockFile.Save(solution.RootPath);
            AnsiConsole.MarkupLine(
                $"\n[green]{changed} updated[/], {unchanged} unchanged, {failed} failed."
            );
        }
        else
        {
            AnsiConsole.MarkupLine(
                $"\n[dim]{changed} would change, {unchanged} unchanged, {failed} failed.[/]"
            );
        }

        if (settings.Check && changed > 0)
        {
            return 2;
        }

        return failed > 0 ? 1 : 0;
    }
}
