using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SimpleModule.Cli.Commands.Maintenance;

/// <summary>
/// File-based sentinel that mirrors what <c>MaintenanceModeMiddleware</c>
/// reads at request time. Kept in the CLI tree so the two sides stay
/// honest about the on-disk shape — the middleware loads via JSON
/// deserialization, and this writes the same JSON shape.
/// </summary>
public static class MaintenanceSentinel
{
    public const string FileName = ".maintenance";

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static string PathFor(string contentRoot) => Path.Combine(contentRoot, FileName);

    public static void Write(
        string contentRoot,
        string? secret,
        string? message,
        int retryAfterSeconds,
        DateTimeOffset? until
    )
    {
        var payload = new
        {
            Until = until,
            SecretHash = secret is null ? null : HashSecret(secret),
            Message = message,
            RetryAfterSeconds = retryAfterSeconds,
        };

        Directory.CreateDirectory(contentRoot);
        File.WriteAllText(PathFor(contentRoot), JsonSerializer.Serialize(payload, JsonOptions));
    }

    public static bool Delete(string contentRoot)
    {
        var path = PathFor(contentRoot);
        if (!File.Exists(path))
        {
            return false;
        }
        File.Delete(path);
        return true;
    }

    public static bool Exists(string contentRoot) => File.Exists(PathFor(contentRoot));

    public static string HashSecret(string secret)
    {
        var bytes = Encoding.UTF8.GetBytes(secret);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexStringLower(hash);
    }
}
