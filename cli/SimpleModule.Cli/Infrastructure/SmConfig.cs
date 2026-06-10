using System.Text.Json;

namespace SimpleModule.Cli.Infrastructure;

/// <summary>
/// Solution-level CLI configuration stored in <c>sm.json</c> at the solution root.
/// The registry URL abstracts the package feed so a marketplace feed can replace
/// nuget.org later without CLI changes.
/// </summary>
public sealed class SmConfig
{
    public const string FileName = "sm.json";
    public const string DefaultRegistry = "https://api.nuget.org/v3/index.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    /// <summary>NuGet V3 service index URL used for search, resolve and download.</summary>
    public string Registry { get; set; } = DefaultRegistry;

    public static SmConfig Load(string solutionRoot)
    {
        var path = Path.Combine(solutionRoot, FileName);
        if (!File.Exists(path))
        {
            return new SmConfig();
        }

        try
        {
            var config = JsonSerializer.Deserialize<SmConfig>(
                File.ReadAllText(path),
                SerializerOptions
            );
            if (config is null || string.IsNullOrWhiteSpace(config.Registry))
            {
                return new SmConfig();
            }

            return config;
        }
        catch (JsonException)
        {
            // A broken sm.json should not brick every command; commands that care
            // can warn. Fall back to the public registry.
            return new SmConfig();
        }
    }

    public void Save(string solutionRoot)
    {
        var path = Path.Combine(solutionRoot, FileName);
        File.WriteAllText(path, JsonSerializer.Serialize(this, SerializerOptions));
    }
}
