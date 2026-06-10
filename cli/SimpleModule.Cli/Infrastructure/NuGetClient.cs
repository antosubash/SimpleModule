using System.Text.Json;
using System.Text.RegularExpressions;

namespace SimpleModule.Cli.Infrastructure;

/// <summary>
/// Minimal NuGet V3 client: resolves the flat-container resource from a service
/// index, lists versions, and downloads nupkgs. Local directory feeds bypass
/// HTTP entirely via <see cref="FindLocalNupkg"/>.
/// </summary>
public static partial class NuGetClient
{
    private static readonly HttpClient SharedHttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(30),
    };

    public static bool IsLocalDirectorySource(string source) =>
        !source.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
        && !source.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Finds <c>{id}.{version}.nupkg</c> in a local folder feed; without a
    /// version the highest one wins. Returns null when absent.
    /// </summary>
    public static string? FindLocalNupkg(string feedDirectory, string packageId, string? version)
    {
        if (!Directory.Exists(feedDirectory))
        {
            return null;
        }

        if (version is not null)
        {
            var exact = Path.Combine(feedDirectory, $"{packageId}.{version}.nupkg");
            return File.Exists(exact) ? exact : null;
        }

        var prefix = packageId + ".";
        var candidates = new List<(string Path, string Version)>();
        foreach (var file in Directory.EnumerateFiles(feedDirectory, prefix + "*.nupkg"))
        {
            var name = Path.GetFileNameWithoutExtension(file);
            var versionPart = name[prefix.Length..];
            // Reject longer package ids (SimpleModule.X must not match
            // SimpleModule.X.Contracts.1.0.0): the remainder must start with a digit.
            if (VersionStartRegex().IsMatch(versionPart))
            {
                candidates.Add((file, versionPart));
            }
        }

        return candidates
            .OrderByDescending(c => c.Version, VersionStringComparer.Instance)
            .Select(c => c.Path)
            .FirstOrDefault();
    }

    public static async Task<IReadOnlyList<string>> GetVersionsAsync(
        Uri serviceIndexUrl,
        string packageId
    )
    {
        var baseUrl = await ResolveFlatContainerAsync(serviceIndexUrl);
        var url = $"{baseUrl}{packageId.ToLowerInvariant()}/index.json";
        using var response = await SharedHttpClient.GetAsync(new Uri(url));
        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return
        [
            .. doc
                .RootElement.GetProperty("versions")
                .EnumerateArray()
                .Select(v => v.GetString() ?? ""),
        ];
    }

    public static async Task DownloadNupkgAsync(
        Uri serviceIndexUrl,
        string packageId,
        string version,
        string destinationPath
    )
    {
        var baseUrl = await ResolveFlatContainerAsync(serviceIndexUrl);
        var idLower = packageId.ToLowerInvariant();
        var versionLower = version.ToLowerInvariant();
        var url = $"{baseUrl}{idLower}/{versionLower}/{idLower}.{versionLower}.nupkg";

        using var response = await SharedHttpClient.GetAsync(new Uri(url));
        response.EnsureSuccessStatusCode();
        await using var file = File.Create(destinationPath);
        await response.Content.CopyToAsync(file);
    }

    private static async Task<string> ResolveFlatContainerAsync(Uri serviceIndexUrl)
    {
        using var response = await SharedHttpClient.GetAsync(serviceIndexUrl);
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        foreach (var resource in doc.RootElement.GetProperty("resources").EnumerateArray())
        {
            var type = resource.GetProperty("@type").GetString() ?? "";
            if (type.StartsWith("PackageBaseAddress/3.0.0", StringComparison.Ordinal))
            {
                var id = resource.GetProperty("@id").GetString() ?? "";
                return id.EndsWith('/') ? id : id + "/";
            }
        }

        throw new InvalidOperationException(
            $"Registry '{serviceIndexUrl}' exposes no PackageBaseAddress/3.0.0 resource."
        );
    }

    /// <summary>Orders dotted version strings numerically with release > prerelease.</summary>
    private sealed class VersionStringComparer : IComparer<string>
    {
        public static readonly VersionStringComparer Instance = new();

        public int Compare(string? x, string? y)
        {
            var (xCore, xPre) = Split(x ?? "");
            var (yCore, yPre) = Split(y ?? "");

            var xParts = xCore.Split('.');
            var yParts = yCore.Split('.');
            for (var i = 0; i < Math.Max(xParts.Length, yParts.Length); i++)
            {
                var xNum = i < xParts.Length && int.TryParse(xParts[i], out var xv) ? xv : 0;
                var yNum = i < yParts.Length && int.TryParse(yParts[i], out var yv) ? yv : 0;
                var byNum = xNum.CompareTo(yNum);
                if (byNum != 0)
                {
                    return byNum;
                }
            }

            if (xPre.Length == 0 && yPre.Length == 0)
            {
                return 0;
            }

            if (xPre.Length == 0)
            {
                return 1;
            }

            if (yPre.Length == 0)
            {
                return -1;
            }

            return string.CompareOrdinal(xPre, yPre);
        }

        private static (string Core, string Prerelease) Split(string version)
        {
            var dash = version.IndexOf('-', StringComparison.Ordinal);
            return dash < 0 ? (version, "") : (version[..dash], version[(dash + 1)..]);
        }
    }

    [GeneratedRegex("^[0-9]")]
    private static partial Regex VersionStartRegex();
}
