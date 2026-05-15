using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SimpleModule.Cli.Commands.Tail;

/// <summary>
/// Parses arbitrary log lines into <see cref="LogEntry"/>. Supports Serilog compact JSON,
/// .NET <c>JsonConsoleFormatter</c>, and a best-effort plain-text fallback.
/// </summary>
public static class LogEntryParser
{
    private static readonly HashSet<string> TimestampKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "@t",
        "Timestamp",
        "timestamp",
    };

    private static readonly HashSet<string> LevelKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "@l",
        "Level",
        "level",
        "LogLevel",
    };

    private static readonly HashSet<string> MessageKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "@m",
        "Message",
        "message",
    };

    private static readonly HashSet<string> MessageTemplateKeys = new(
        StringComparer.OrdinalIgnoreCase
    )
    {
        "@mt",
        "MessageTemplate",
    };

    private static readonly HashSet<string> SourceKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "SourceContext",
        "Category",
        "category",
        "Logger",
        "logger",
    };

    // e.g. "2024-01-02 03:04:05.123 +00:00 [INF] Foo.Bar: hello world"
    // or   "[12:34:56 INF Foo.Bar] hello world"
    // or   "2024-01-02T03:04:05.123Z INFO Foo.Bar - hello"
    private static readonly Regex PlainTextRegex = new(
        """
        ^\s*
        (?:\[?(?<ts>\d{4}-\d{2}-\d{2}[ Tt]\d{2}:\d{2}:\d{2}(?:[.,]\d+)?(?:\s*[+-]\d{2}:?\d{2}|Z)?)\]?
            |\[(?<ts>\d{2}:\d{2}:\d{2}(?:[.,]\d+)?)\])
        \s*
        \[?(?<lvl>TRACE|TRC|DEBUG|DBG|INFO|INFORMATION|INF|WARN|WARNING|WRN|ERROR|ERR|CRITICAL|CRT|FATAL|FTL)\]?
        \s*
        (?:(?<src>[A-Za-z_][\w.]*)\s*[:\-])?
        \s*(?<msg>.*)$
        """,
        RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled
    );

    public static LogEntry Parse(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return new LogEntry { Raw = line, Message = line };
        }

        var trimmed = line.TrimStart();
        if (trimmed.StartsWith('{') && TryParseJson(line, out var jsonEntry))
        {
            return jsonEntry;
        }

        return ParsePlain(line);
    }

    public static bool TryParseJson(string line, out LogEntry entry)
    {
        try
        {
            using var doc = JsonDocument.Parse(line);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                entry = new LogEntry { Raw = line, Message = line };
                return false;
            }

            DateTimeOffset? timestamp = null;
            string? level = null;
            string? source = null;
            string? message = null;
            string? messageTemplate = null;
            var properties = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (TimestampKeys.Contains(prop.Name))
                {
                    if (
                        prop.Value.ValueKind == JsonValueKind.String
                        && DateTimeOffset.TryParse(
                            prop.Value.GetString(),
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.AssumeUniversal,
                            out var parsed
                        )
                    )
                    {
                        timestamp = parsed;
                    }
                    continue;
                }

                if (LevelKeys.Contains(prop.Name))
                {
                    level = ReadString(prop.Value);
                    continue;
                }

                if (SourceKeys.Contains(prop.Name))
                {
                    source = ReadString(prop.Value);
                    continue;
                }

                if (MessageKeys.Contains(prop.Name))
                {
                    message = ReadString(prop.Value);
                    continue;
                }

                if (MessageTemplateKeys.Contains(prop.Name))
                {
                    messageTemplate = ReadString(prop.Value);
                    continue;
                }

                // .NET JsonConsoleFormatter wraps structured properties under "State"
                if (
                    prop.Name.Equals("State", StringComparison.OrdinalIgnoreCase)
                    && prop.Value.ValueKind == JsonValueKind.Object
                )
                {
                    foreach (var stateProp in prop.Value.EnumerateObject())
                    {
                        if (stateProp.Name.Equals("Message", StringComparison.OrdinalIgnoreCase))
                        {
                            message ??= ReadString(stateProp.Value);
                            continue;
                        }
                        if (
                            stateProp.Name.Equals(
                                "{OriginalFormat}",
                                StringComparison.OrdinalIgnoreCase
                            )
                        )
                        {
                            messageTemplate ??= ReadString(stateProp.Value);
                            continue;
                        }
                        properties[stateProp.Name] = ReadString(stateProp.Value);
                    }
                    continue;
                }

                // EventId object → record EventId.Id / EventId.Name
                if (
                    prop.Name.Equals("EventId", StringComparison.OrdinalIgnoreCase)
                    && prop.Value.ValueKind == JsonValueKind.Object
                )
                {
                    foreach (var evtProp in prop.Value.EnumerateObject())
                    {
                        properties["EventId." + evtProp.Name] = ReadString(evtProp.Value);
                    }
                    continue;
                }

                properties[prop.Name] = ReadString(prop.Value);
            }

            entry = new LogEntry
            {
                Timestamp = timestamp,
                Level = level,
                Source = source,
                Message = message ?? messageTemplate ?? line,
                Raw = line,
                Properties = properties,
            };
            return true;
        }
        catch (JsonException)
        {
            entry = new LogEntry { Raw = line, Message = line };
            return false;
        }
    }

    public static LogEntry ParsePlain(string line)
    {
        var match = PlainTextRegex.Match(line);
        if (!match.Success)
        {
            return new LogEntry { Raw = line, Message = line };
        }

        DateTimeOffset? timestamp = null;
        var tsText = match.Groups["ts"].Value;
        if (
            !string.IsNullOrEmpty(tsText)
            && DateTimeOffset.TryParse(
                tsText,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal,
                out var parsedTs
            )
        )
        {
            timestamp = parsedTs;
        }

        var lvl = match.Groups["lvl"].Value;
        var src = match.Groups["src"].Success ? match.Groups["src"].Value : null;
        var msg = match.Groups["msg"].Value;

        return new LogEntry
        {
            Timestamp = timestamp,
            Level = NormaliseLevel(lvl),
            Source = string.IsNullOrEmpty(src) ? null : src,
            Message = msg,
            Raw = line,
        };
    }

    private static string? ReadString(JsonElement element) =>
        element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => null,
            _ => element.GetRawText(),
        };

    private static string? NormaliseLevel(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return raw.ToUpperInvariant() switch
        {
            "TRC" or "TRACE" => "Trace",
            "DBG" or "DEBUG" => "Debug",
            "INF" or "INFO" or "INFORMATION" => "Information",
            "WRN" or "WARN" or "WARNING" => "Warning",
            "ERR" or "ERROR" => "Error",
            "CRT" or "CRITICAL" or "FTL" or "FATAL" => "Critical",
            _ => raw,
        };
    }
}
