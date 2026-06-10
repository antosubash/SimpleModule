using System.Text.Json;

namespace SimpleModule.Cli.Infrastructure;

/// <summary>
/// CLI-local view of the module manifest (schema v1) emitted by
/// SimpleModule.Generator. Kept independent of SimpleModule.Core so the CLI
/// has no framework assembly dependency.
/// </summary>
public sealed class ModuleManifestData
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public int SchemaVersion { get; init; }
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public string Version { get; init; } = "";
    public string FrameworkCompat { get; init; } = "";
    public string RoutePrefix { get; init; } = "";
    public string ViewPrefix { get; init; } = "";
    public string Schema { get; init; } = "";
    public IReadOnlyList<string> Permissions { get; init; } = [];
    public string? FrontendEntry { get; init; }
    public IReadOnlyList<string> Pages { get; init; } = [];
    public IReadOnlyList<string> EventsPublished { get; init; } = [];
    public IReadOnlyList<string> EventsConsumed { get; init; } = [];
    public bool HasDbContext { get; init; }

    public static ModuleManifestData? TryParse(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<ModuleManifestData>(json, SerializerOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
