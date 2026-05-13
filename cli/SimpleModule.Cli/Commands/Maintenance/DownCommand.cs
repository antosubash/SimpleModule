using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using SimpleModule.Cli.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SimpleModule.Cli.Commands.Maintenance;

public sealed class DownCommand : Command<DownSettings>
{
    public override int Execute(CommandContext context, DownSettings settings)
    {
        var solution = SolutionContext.Discover();
        if (solution is null)
        {
            AnsiConsole.MarkupLine(
                "[red]Could not find .slnx file. Run this command from within a SimpleModule project.[/]"
            );
            return 1;
        }

        var sentinelPath = MaintenanceSentinelFile.ResolvePath(solution);

        if (settings.Status)
        {
            return PrintStatus(sentinelPath);
        }

        DateTimeOffset? until = null;
        if (!string.IsNullOrWhiteSpace(settings.Until))
        {
            if (
                !DateTimeOffset.TryParse(
                    settings.Until,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var parsed
                )
            )
            {
                AnsiConsole.MarkupLine(
                    $"[red]--until value '{settings.Until}' is not a valid ISO-8601 timestamp.[/]"
                );
                return 1;
            }
            until = parsed;
        }

        var secret = settings.Secret ?? GenerateSecret();
        var retryAfter = settings.RetryAfterSeconds is > 0 ? settings.RetryAfterSeconds.Value : 60;

        var sentinel = new MaintenanceSentinel
        {
            SecretHash = MaintenanceSentinelFile.HashSecret(secret),
            Message = settings.Message,
            RetryAfterSeconds = retryAfter,
            Until = until,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var dir = Path.GetDirectoryName(sentinelPath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.WriteAllText(
            sentinelPath,
            JsonSerializer.Serialize(sentinel, MaintenanceSentinelFile.JsonOptions)
        );

        AnsiConsole.MarkupLine("[green]Application is now in maintenance mode.[/]");
        AnsiConsole.MarkupLine($"  Sentinel: [grey]{sentinelPath.EscapeMarkup()}[/]");
        AnsiConsole.MarkupLine($"  Bypass URL: [yellow]/?sm_bypass={secret.EscapeMarkup()}[/]");
        if (settings.Secret is null)
        {
            AnsiConsole.MarkupLine(
                "  [grey](no --secret provided; a fresh one was generated above)[/]"
            );
        }
        if (until is { } u)
        {
            AnsiConsole.MarkupLine(
                $"  Auto-clears at: [grey]{u.ToString("u", CultureInfo.InvariantCulture).EscapeMarkup()}[/]"
            );
        }

        return 0;
    }

    private static int PrintStatus(string sentinelPath)
    {
        var sentinel = MaintenanceSentinelFile.TryRead(sentinelPath);
        if (sentinel is null)
        {
            AnsiConsole.MarkupLine("[green]Application is up.[/]");
            AnsiConsole.MarkupLine($"  Sentinel: [grey]{sentinelPath.EscapeMarkup()}[/] (absent)");
            return 0;
        }

        AnsiConsole.MarkupLine("[yellow]Application is in maintenance mode.[/]");
        AnsiConsole.MarkupLine($"  Sentinel: [grey]{sentinelPath.EscapeMarkup()}[/]");
        AnsiConsole.MarkupLine(
            $"  Created at: [grey]{sentinel.CreatedAt.ToString("u", CultureInfo.InvariantCulture).EscapeMarkup()}[/]"
        );
        AnsiConsole.MarkupLine($"  Retry-After: [grey]{sentinel.RetryAfterSeconds}s[/]");
        if (!string.IsNullOrWhiteSpace(sentinel.Message))
        {
            AnsiConsole.MarkupLine($"  Message: [grey]{sentinel.Message.EscapeMarkup()}[/]");
        }
        if (sentinel.Until is { } u)
        {
            AnsiConsole.MarkupLine(
                $"  Until: [grey]{u.ToString("u", CultureInfo.InvariantCulture).EscapeMarkup()}[/]"
            );
        }
        return 0;
    }

    private static string GenerateSecret()
    {
        Span<byte> bytes = stackalloc byte[16];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexStringLower(bytes);
    }
}
