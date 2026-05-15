using System.Collections.Generic;

namespace SimpleModule.Cli.Commands.Tail;

/// <summary>
/// Normalised representation of a single log line, regardless of its source format
/// (Serilog compact JSON, .NET <c>JsonConsoleFormatter</c>, or arbitrary plain text).
/// </summary>
public sealed record LogEntry
{
    public DateTimeOffset? Timestamp { get; init; }

    public string? Level { get; init; }

    public string? Source { get; init; }

    public string? Message { get; init; }

    public string? Raw { get; init; }

    public IReadOnlyDictionary<string, string?> Properties { get; init; } =
        new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
}
