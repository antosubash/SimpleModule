using System.Diagnostics;

namespace SimpleModule.Cli.Commands.Dev;

public sealed partial class DevCommand
{
    private static int GetSafePid(Process process)
    {
        try
        {
            return process.Id;
        }
#pragma warning disable CA1031 // Do not catch general exception types
        catch
        {
            return -1;
        }
#pragma warning restore CA1031
    }

    /// <summary>
    /// Parse launchSettings.json to find the ports ASP.NET will bind to.
    /// Falls back to the default ports (5001, 5000) if the file can't be read.
    /// </summary>
    private static List<int> DiscoverDotnetPorts(string hostProjectPath)
    {
        var ports = new List<int>();
        var hostDir = Path.GetDirectoryName(hostProjectPath);
        if (hostDir is null)
        {
            return [5001, 5000];
        }

        var launchSettingsPath = Path.Combine(hostDir, "Properties", "launchSettings.json");

        if (!File.Exists(launchSettingsPath))
        {
            return [5001, 5000];
        }

        try
        {
            var json = File.ReadAllText(launchSettingsPath);

            // Extract applicationUrl values and parse ports from them.
            // Format: "applicationUrl": "https://localhost:5001;http://localhost:5000"
            // Use simple string parsing to avoid adding a JSON dependency to the CLI.
            var searchKey = "\"applicationUrl\"";
            var idx = json.IndexOf(searchKey, StringComparison.OrdinalIgnoreCase);
            while (idx >= 0)
            {
                var colonIdx = json.IndexOf(':', idx + searchKey.Length);
                if (colonIdx < 0)
                {
                    break;
                }

                var quoteStart = json.IndexOf('"', colonIdx + 1);
                if (quoteStart < 0)
                {
                    break;
                }

                var quoteEnd = json.IndexOf('"', quoteStart + 1);
                if (quoteEnd < 0)
                {
                    break;
                }

                var urlValue = json[(quoteStart + 1)..quoteEnd];
                foreach (var url in urlValue.Split(';'))
                {
                    // Extract port from URL like "https://localhost:5001"
                    var lastColon = url.LastIndexOf(':');
                    if (
                        lastColon >= 0
                        && int.TryParse(
                            url[(lastColon + 1)..],
                            System.Globalization.NumberStyles.Integer,
                            System.Globalization.CultureInfo.InvariantCulture,
                            out var port
                        )
                    )
                    {
                        if (!ports.Contains(port))
                        {
                            ports.Add(port);
                        }
                    }
                }

                idx = json.IndexOf(searchKey, quoteEnd + 1, StringComparison.OrdinalIgnoreCase);
            }
        }
#pragma warning disable CA1031 // Do not catch general exception types
        catch
        {
            // If we can't parse, fall back to defaults
        }
#pragma warning restore CA1031

        return ports.Count > 0 ? ports : [5001, 5000];
    }
}
