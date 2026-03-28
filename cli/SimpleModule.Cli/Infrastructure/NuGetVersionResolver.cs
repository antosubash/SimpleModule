using System.Text.Json;

namespace SimpleModule.Cli.Infrastructure;

public static class NuGetVersionResolver
{
    private const string FallbackVersion = "0.0.15";
    private static readonly Uri NuGetIndexUri =
        new("https://api.nuget.org/v3-flatcontainer/simplemodule.core/index.json");

    // Shared HttpClient avoids socket exhaustion from repeated instantiation.
    private static readonly HttpClient SharedHttpClient = new() { Timeout = TimeSpan.FromSeconds(5) };

    /// <summary>
    /// Resolves the SimpleModule framework version to use for a new project.
    /// Priority: explicit version > NuGet API > version.json > hardcoded fallback.
    /// </summary>
    public static string ResolveVersion(
        string? explicitVersion = null,
        SolutionContext? solution = null
    )
    {
        if (!string.IsNullOrWhiteSpace(explicitVersion))
        {
            return explicitVersion;
        }

        var nugetVersion = QueryNuGetLatestVersion();
        if (nugetVersion is not null)
        {
            return nugetVersion;
        }

        if (solution is not null)
        {
            var versionFromFile = ReadVersionJson(solution.RootPath);
            if (versionFromFile is not null)
            {
                return versionFromFile;
            }
        }

        return FallbackVersion;
    }

    private static string? QueryNuGetLatestVersion()
    {
        try
        {
            // Intentional sync-over-async: CLI runs single-threaded, no deadlock risk.
            var json = SharedHttpClient.GetStringAsync(NuGetIndexUri).GetAwaiter().GetResult();
            using var doc = JsonDocument.Parse(json);

            if (
                doc.RootElement.TryGetProperty("versions", out var versions)
                && versions.ValueKind == JsonValueKind.Array
            )
            {
                var count = versions.GetArrayLength();
                if (count > 0)
                {
                    return versions[count - 1].GetString();
                }
            }
        }
        catch (HttpRequestException)
        {
            // Network errors — fall through
        }
        catch (TaskCanceledException)
        {
            // Timeout — fall through
        }
        catch (JsonException)
        {
            // Parse errors — fall through
        }

        return null;
    }

    private static string? ReadVersionJson(string rootPath)
    {
        try
        {
            var path = Path.Combine(rootPath, "version.json");
            var json = File.ReadAllText(path);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("version", out var version))
            {
                return version.GetString();
            }
        }
        catch (JsonException)
        {
            // Parse errors — fall through
        }
        catch (IOException)
        {
            // File read errors — fall through
        }

        return null;
    }
}
