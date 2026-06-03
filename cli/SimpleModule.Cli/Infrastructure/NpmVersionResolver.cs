using System.Text.Json;

namespace SimpleModule.Cli.Infrastructure;

/// <summary>
/// Resolves the latest <em>published</em> version of the <c>@simplemodule/*</c> npm packages.
/// The npm packages are not always published in lockstep with the NuGet framework version, so a
/// scaffold must not assume the framework (NuGet) version exists on npm — otherwise
/// <c>npm install</c> fails with <c>notarget</c> (see issue #219).
/// </summary>
public static class NpmVersionResolver
{
    private static readonly Uri NpmRegistryUri = new(
        "https://registry.npmjs.org/@simplemodule/client"
    );

    // Shared HttpClient avoids socket exhaustion from repeated instantiation.
    private static readonly HttpClient SharedHttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(5),
    };

    /// <summary>
    /// Resolves the npm version to pin the <c>@simplemodule/*</c> dependencies to.
    /// Priority: npm registry <c>dist-tags.latest</c> &gt; <paramref name="fallbackVersion"/>.
    /// </summary>
    /// <param name="fallbackVersion">
    /// Version to use when the registry can't be reached (typically the framework version).
    /// </param>
    public static string ResolveVersion(string fallbackVersion)
    {
        return QueryNpmLatestVersion() ?? fallbackVersion;
    }

    private static string? QueryNpmLatestVersion()
    {
        try
        {
            // Intentional sync-over-async: CLI runs single-threaded, no deadlock risk.
            var json = SharedHttpClient.GetStringAsync(NpmRegistryUri).GetAwaiter().GetResult();
            using var doc = JsonDocument.Parse(json);

            if (
                doc.RootElement.TryGetProperty("dist-tags", out var distTags)
                && distTags.TryGetProperty("latest", out var latest)
            )
            {
                return latest.GetString();
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
}
