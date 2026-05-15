using System.Globalization;
using System.Text;
using Spectre.Console;

namespace SimpleModule.Cli.Commands.Tail;

/// <summary>
/// Renders a parsed <see cref="LogEntry"/> to an <see cref="IAnsiConsole"/>.
/// </summary>
public static class LogEntryRenderer
{
    public static void Render(
        IAnsiConsole console,
        LogEntry entry,
        bool useColor,
        string? filePrefix = null
    )
    {
        ArgumentNullException.ThrowIfNull(console);
        ArgumentNullException.ThrowIfNull(entry);

        var line = useColor ? BuildMarkup(entry, filePrefix) : BuildPlain(entry, filePrefix);
        if (useColor)
        {
            console.MarkupLine(line);
        }
        else
        {
            console.WriteLine(line);
        }
    }

    private static string BuildMarkup(LogEntry entry, string? filePrefix)
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrEmpty(filePrefix))
        {
            sb.Append("[grey]").Append(Markup.Escape($"[{filePrefix}]")).Append("[/] ");
        }

        if (entry.Timestamp.HasValue)
        {
            var ts = entry
                .Timestamp.Value.ToLocalTime()
                .ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
            sb.Append("[grey]").Append(Markup.Escape(ts)).Append("[/] ");
        }

        var lvl = entry.Level ?? "Information";
        var lvlColor = ColorForLevel(lvl);
        var lvlShort = ShortLevel(lvl);
        sb.Append('[').Append(lvlColor).Append(']').Append(Markup.Escape(lvlShort)).Append("[/] ");

        if (!string.IsNullOrEmpty(entry.Source))
        {
            sb.Append("[italic grey]").Append(Markup.Escape(entry.Source)).Append("[/] ");
        }

        sb.Append(Markup.Escape(entry.Message ?? string.Empty));

        if (entry.Properties.Count > 0)
        {
            var rendered = RenderProperties(entry);
            if (!string.IsNullOrEmpty(rendered))
            {
                sb.Append(" [dim]").Append(Markup.Escape(rendered)).Append("[/]");
            }
        }

        return sb.ToString();
    }

    private static string BuildPlain(LogEntry entry, string? filePrefix)
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrEmpty(filePrefix))
        {
            sb.Append('[').Append(filePrefix).Append("] ");
        }

        if (entry.Timestamp.HasValue)
        {
            sb.Append(
                    entry
                        .Timestamp.Value.ToLocalTime()
                        .ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture)
                )
                .Append(' ');
        }

        sb.Append(ShortLevel(entry.Level ?? "Information")).Append(' ');

        if (!string.IsNullOrEmpty(entry.Source))
        {
            sb.Append(entry.Source).Append(' ');
        }

        sb.Append(entry.Message ?? string.Empty);

        if (entry.Properties.Count > 0)
        {
            var rendered = RenderProperties(entry);
            if (!string.IsNullOrEmpty(rendered))
            {
                sb.Append(' ').Append(rendered);
            }
        }

        return sb.ToString();
    }

    private static string RenderProperties(LogEntry entry)
    {
        var parts = new List<string>(entry.Properties.Count);
        foreach (var kvp in entry.Properties)
        {
            if (string.IsNullOrEmpty(kvp.Value))
            {
                continue;
            }
            parts.Add(kvp.Key + "=" + kvp.Value);
        }
        if (parts.Count == 0)
        {
            return string.Empty;
        }
        return "{ " + string.Join(", ", parts) + " }";
    }

    private static string ColorForLevel(string level) =>
        level.Trim().ToUpperInvariant() switch
        {
            "TRC" or "TRACE" => "grey50",
            "DBG" or "DEBUG" => "grey",
            "INF" or "INFO" or "INFORMATION" => "cyan",
            "WRN" or "WARN" or "WARNING" => "yellow",
            "ERR" or "ERROR" => "red",
            "CRT" or "CRITICAL" or "FTL" or "FATAL" => "red bold",
            _ => "white",
        };

    private static string ShortLevel(string level) =>
        level.Trim().ToUpperInvariant() switch
        {
            "TRC" or "TRACE" => "TRC",
            "DBG" or "DEBUG" => "DBG",
            "INF" or "INFO" or "INFORMATION" => "INF",
            "WRN" or "WARN" or "WARNING" => "WRN",
            "ERR" or "ERROR" => "ERR",
            "CRT" or "CRITICAL" => "CRT",
            "FTL" or "FATAL" => "FTL",
            _ => level.Length >= 3 ? level[..3].ToUpperInvariant() : level.ToUpperInvariant(),
        };
}
