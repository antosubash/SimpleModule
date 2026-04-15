using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace SimpleModule.Marketplace;

[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "Instantiated by JSON deserialization"
)]
internal sealed record NuGetSearchResponse
{
    [JsonPropertyName("totalHits")]
    public int TotalHits { get; init; }

    [JsonPropertyName("data")]
    public List<NuGetPackageData> Data { get; init; } = [];
}

[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "Instantiated by JSON deserialization"
)]
internal sealed record NuGetPackageData
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("version")]
    public string? Version { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("authors")]
    public List<string>? Authors { get; init; }

    [JsonPropertyName("iconUrl")]
    public string? IconAddress { get; init; }

    [JsonPropertyName("totalDownloads")]
    public long TotalDownloads { get; init; }

    [JsonPropertyName("tags")]
    public List<string>? Tags { get; init; }

    [JsonPropertyName("projectUrl")]
    public string? ProjectAddress { get; init; }

    [JsonPropertyName("licenseUrl")]
    public string? LicenseAddress { get; init; }

    [JsonPropertyName("versions")]
    public List<NuGetVersionData>? Versions { get; init; }
}

[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "Instantiated by JSON deserialization"
)]
internal sealed record NuGetVersionData
{
    [JsonPropertyName("version")]
    public string? Version { get; init; }

    [JsonPropertyName("downloads")]
    public long Downloads { get; init; }
}
