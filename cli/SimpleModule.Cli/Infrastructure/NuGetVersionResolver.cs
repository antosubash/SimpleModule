using System.Text.Json;

namespace SimpleModule.Cli.Infrastructure;

public static class NuGetVersionResolver
{
    private const string FallbackVersion = "0.0.15";
    private const string NuGetIndexUrl =
        "https://api.nuget.org/v3-flatcontainer/simplemodule.core/index.json";

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
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var json = client.GetStringAsync(new Uri(NuGetIndexUrl)).GetAwaiter().GetResult();
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
            if (!File.Exists(path))
            {
                return null;
            }

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
