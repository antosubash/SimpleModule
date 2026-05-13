using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using SimpleModule.Cli.Infrastructure;

namespace SimpleModule.Cli.Commands.Maintenance;

/// <summary>
/// File-format mirror of <c>SimpleModule.Hosting.Maintenance.MaintenanceModeState</c>.
/// Kept here so the CLI doesn't have to reference the hosting framework — the JSON
/// shape is the contract between them.
/// </summary>
public sealed record MaintenanceSentinel
{
    public string? SecretHash { get; init; }
    public string? Message { get; init; }
    public int RetryAfterSeconds { get; init; } = 60;
    public DateTimeOffset? Until { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}

public static class MaintenanceSentinelFile
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static string ResolvePath(SolutionContext solution)
    {
        var hostDir =
            Path.GetDirectoryName(solution.ApiCsprojPath)
            ?? throw new InvalidOperationException(
                "Host project directory could not be resolved from solution context."
            );
        return Path.Combine(hostDir, "App_Data", "maintenance.json");
    }

    public static string HashSecret(string secret)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(secret));
        return Convert.ToHexStringLower(bytes);
    }

    public static MaintenanceSentinel? TryRead(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }
        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<MaintenanceSentinel>(json, JsonOptions);
        }
        catch (IOException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
