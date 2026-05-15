namespace SimpleModule.Cli.Commands.Tail;

/// <summary>
/// Stateless predicates that decide whether a <see cref="LogEntry"/> matches a given
/// <see cref="TailSettings"/>.
/// </summary>
public static class LogEntryFilter
{
    public static bool Matches(LogEntry entry, TailSettings settings)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(settings);

        if (!MatchesLevel(entry, settings.Level))
        {
            return false;
        }

        if (!MatchesSubstring(entry.Message, settings.Filter))
        {
            return false;
        }

        if (!MatchesSource(entry.Source, settings.Source))
        {
            return false;
        }

        if (!MatchesProperty(entry, "UserId", settings.User))
        {
            return false;
        }

        if (!MatchesProperty(entry, "RequestId", settings.Request))
        {
            return false;
        }

        return true;
    }

    public static int LevelRank(string? level)
    {
        if (string.IsNullOrWhiteSpace(level))
        {
            return -1;
        }

        return level.Trim().ToUpperInvariant() switch
        {
            "TRC" or "TRACE" or "VERBOSE" or "VRB" => 0,
            "DBG" or "DEBUG" => 1,
            "INF" or "INFO" or "INFORMATION" => 2,
            "WRN" or "WARN" or "WARNING" => 3,
            "ERR" or "ERROR" => 4,
            "CRT" or "CRITICAL" or "FTL" or "FATAL" => 5,
            _ => -1,
        };
    }

    private static bool MatchesLevel(LogEntry entry, string? minLevel)
    {
        if (string.IsNullOrWhiteSpace(minLevel))
        {
            return true;
        }

        var threshold = LevelRank(minLevel);
        if (threshold < 0)
        {
            // Unrecognised level filter — let everything through rather than dropping silently.
            return true;
        }

        var entryRank = LevelRank(entry.Level);
        if (entryRank < 0)
        {
            // We don't know the entry's level — keep it visible.
            return true;
        }

        return entryRank >= threshold;
    }

    private static bool MatchesSubstring(string? haystack, string? needle)
    {
        if (string.IsNullOrEmpty(needle))
        {
            return true;
        }

        if (string.IsNullOrEmpty(haystack))
        {
            return false;
        }

        return haystack.Contains(needle, StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesSource(string? entrySource, string? filterSource)
    {
        if (string.IsNullOrWhiteSpace(filterSource))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(entrySource))
        {
            return false;
        }

        if (entrySource.Equals(filterSource, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Treat filter as a namespace prefix when separated by '.'
        if (
            entrySource.StartsWith(filterSource + ".", StringComparison.OrdinalIgnoreCase)
            || entrySource.StartsWith(filterSource, StringComparison.OrdinalIgnoreCase)
        )
        {
            return true;
        }

        return false;
    }

    private static bool MatchesProperty(LogEntry entry, string propertyName, string? expected)
    {
        if (string.IsNullOrWhiteSpace(expected))
        {
            return true;
        }

        foreach (var kvp in entry.Properties)
        {
            if (kvp.Key.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(kvp.Value))
                {
                    continue;
                }

                if (kvp.Value.Equals(expected, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
