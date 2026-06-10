using System.Security.Cryptography;
using System.Text;

namespace SimpleModule.Cli.Infrastructure;

/// <summary>
/// Ensures a local folder feed is registered in the solution's nuget.config so
/// restores can resolve module packages added from a local source.
/// </summary>
public static class NuGetConfigManipulator
{
    public static void EnsureLocalSource(string solutionRoot, string feedDirectory)
    {
        var configPath = Path.Combine(solutionRoot, "nuget.config");
        var normalizedFeed = Path.GetFullPath(feedDirectory);

        if (!File.Exists(configPath))
        {
            File.WriteAllText(
                configPath,
                $"""
                <?xml version="1.0" encoding="utf-8"?>
                <configuration>
                  <packageSources>
                    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
                    <add key="{SourceKey(normalizedFeed)}" value="{normalizedFeed}" />
                  </packageSources>
                </configuration>
                """
            );
            return;
        }

        var content = File.ReadAllText(configPath);
        if (content.Contains(normalizedFeed, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var lines = File.ReadAllLines(configPath).ToList();
        var close = lines.FindIndex(l => l.Contains("</packageSources>", StringComparison.Ordinal));
        if (close < 0)
        {
            throw new InvalidOperationException(
                $"{configPath} has no <packageSources> section; add the feed '{normalizedFeed}' manually."
            );
        }

        lines.Insert(
            close,
            $"    <add key=\"{SourceKey(normalizedFeed)}\" value=\"{normalizedFeed}\" />"
        );
        File.WriteAllLines(configPath, lines);
    }

    private static string SourceKey(string feedDirectory)
    {
        // Key on the full path (hashed) — two different feeds sharing a leaf
        // directory name must not produce duplicate <add> keys, which NuGet
        // rejects when parsing the config.
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(feedDirectory)));
#pragma warning disable CA1308 // lowercase is conventional for nuget.config keys
        return "sm-local-" + hash[..8].ToLowerInvariant();
#pragma warning restore CA1308
    }
}
