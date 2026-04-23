using Spectre.Console;

namespace SimpleModule.Cli.Infrastructure;

public static class FileTreeRenderer
{
    public static void Render(
        string rootLabel,
        IEnumerable<(string Path, FileAction Action)> ops,
        string? rootDir = null,
        bool isDryRun = false
    )
    {
        if (isDryRun)
        {
            AnsiConsole.MarkupLine("[dim]Dry run — no files written[/]\n");
        }
        else
        {
            AnsiConsole.MarkupLine("");
        }

        var tree = new Tree($"[blue]{Markup.Escape(rootLabel)}[/]");
        foreach (var (path, action) in ops)
        {
            tree.AddNode(FormatNode(path, action, rootDir, isDryRun));
        }
        AnsiConsole.Write(tree);
    }

    internal static string FormatNode(
        string path,
        FileAction action,
        string? rootDir,
        bool isDryRun
    )
    {
        var display = rootDir is null
            ? Path.GetFileName(path)
            : Path.GetRelativePath(rootDir, path).Replace('\\', '/');
        var escaped = Markup.Escape(display);

        return action == FileAction.Modify
                ? $"[yellow]{escaped}[/] [dim]({(isDryRun ? "modify" : "modified")})[/]"
            : isDryRun ? $"[green]{escaped}[/] [dim](create)[/]"
            : $"[green]{escaped}[/]";
    }
}
