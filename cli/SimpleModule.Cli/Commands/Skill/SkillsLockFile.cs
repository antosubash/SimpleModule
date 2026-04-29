using System.Text.Json;
using System.Text.Json.Serialization;

namespace SimpleModule.Cli.Commands.Skill;

public sealed class SkillsLockFile
{
    public const string FileName = "skills-lock.json";
    public const int CurrentVersion = 1;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    [JsonPropertyName("version")]
    public int Version { get; set; } = CurrentVersion;

    [JsonPropertyName("skills")]
    public Dictionary<string, SkillsLockEntry> Skills { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);

    public static string GetPath(string solutionRoot) => Path.Combine(solutionRoot, FileName);

    public static SkillsLockFile Load(string solutionRoot)
    {
        var path = GetPath(solutionRoot);
        if (!File.Exists(path))
        {
            return new SkillsLockFile();
        }

        var json = File.ReadAllText(path);
        if (string.IsNullOrWhiteSpace(json))
        {
            return new SkillsLockFile();
        }

        var loaded = JsonSerializer.Deserialize<SkillsLockFile>(json, SerializerOptions);
        return loaded ?? new SkillsLockFile();
    }

    public void Save(string solutionRoot)
    {
        Version = CurrentVersion;
        var path = GetPath(solutionRoot);
        var ordered = new SkillsLockFile { Version = Version };
        foreach (var key in Skills.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
        {
            ordered.Skills[key] = Skills[key];
        }

        var json = JsonSerializer.Serialize(ordered, SerializerOptions);
        File.WriteAllText(path, json + Environment.NewLine);
    }
}

public sealed class SkillsLockEntry
{
    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    [JsonPropertyName("sourceType")]
    public string SourceType { get; set; } = string.Empty;

    [JsonPropertyName("ref")]
    public string? Ref { get; set; }

    [JsonPropertyName("computedHash")]
    public string ComputedHash { get; set; } = string.Empty;
}
