using System.Diagnostics;
using Spectre.Console;

namespace SimpleModule.PerfSeeder.Seeders;

internal static class SeederProgress
{
    public static void Report(string label, long inserted, long total, Stopwatch sw)
    {
        var elapsed = sw.Elapsed.TotalSeconds;
        var rate = elapsed > 0 ? inserted / elapsed : 0;
        var pct = total > 0 ? (double)inserted / total * 100 : 0;
        AnsiConsole.MarkupLine(
            $"[dim]  {label.EscapeMarkup()}:[/] "
                + $"{inserted:N0}/{total:N0} "
                + $"[green]({pct:F1}%)[/] "
                + $"[dim]{rate:N0} rows/s[/]"
        );
    }

    public static void Final(string label, long inserted, Stopwatch sw)
    {
        sw.Stop();
        var elapsed = sw.Elapsed.TotalSeconds;
        var rate = elapsed > 0 ? inserted / elapsed : 0;
        AnsiConsole.MarkupLine(
            $"[green][[ok]][/] {label.EscapeMarkup()}: "
                + $"inserted {inserted:N0} rows in {elapsed:F1}s "
                + $"[dim]({rate:N0} rows/s)[/]"
        );
    }
}
