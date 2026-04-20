using System.Runtime.InteropServices;

namespace SimpleModule.DevTools;

public sealed partial class ViteDevWatchService
{
    internal static string? FindRepoRoot(string startPath)
    {
        var current = startPath;
        while (current is not null)
        {
            var gitPath = Path.Combine(current, ".git");
            // .git is a directory in normal repos, but a file in worktrees
            if (Directory.Exists(gitPath) || File.Exists(gitPath))
            {
                return current;
            }

            current = Path.GetDirectoryName(current);
        }

        return null;
    }

    internal static List<string> DiscoverModuleDirectories(string modulesRoot)
    {
        var directories = new List<string>();

        if (!Directory.Exists(modulesRoot))
        {
            return directories;
        }

        foreach (var moduleGroupDir in Directory.GetDirectories(modulesRoot))
        {
            var srcDir = Path.Combine(moduleGroupDir, "src");
            if (!Directory.Exists(srcDir))
            {
                continue;
            }

            foreach (var moduleDir in Directory.GetDirectories(srcDir))
            {
                if (File.Exists(Path.Combine(moduleDir, "vite.config.ts")))
                {
                    directories.Add(moduleDir);
                }
            }
        }

        return directories;
    }

    internal static bool ShouldIgnoreModulePath(string fullPath)
    {
        return ContainsSegment(fullPath, "wwwroot") || ContainsSegment(fullPath, "node_modules");
    }

    internal static bool ShouldIgnoreClientAppPath(string fullPath)
    {
        return ContainsSegment(fullPath, "node_modules");
    }

    internal static bool ShouldIgnoreTailwindPath(string fullPath)
    {
        return ContainsSegment(fullPath, "_scan");
    }

    internal static bool ContainsSegment(string fullPath, string segment)
    {
        // Check both separator styles for cross-platform path matching
        return fullPath.Contains(
                $"{Path.DirectorySeparatorChar}{segment}{Path.DirectorySeparatorChar}",
                StringComparison.OrdinalIgnoreCase
            )
            || fullPath.Contains(
                $"{Path.AltDirectorySeparatorChar}{segment}{Path.AltDirectorySeparatorChar}",
                StringComparison.OrdinalIgnoreCase
            );
    }

    private static string GetShellFileName()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "cmd" : "sh";
    }

    private static string GetShellArguments(string command)
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? $"/c {command}"
            : $"-c \"{command}\"";
    }
}
