using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SimpleModule.Marketplace.Contracts;

namespace SimpleModule.Marketplace;

public class NuGetMarketplaceService(
    IHttpClientFactory httpClientFactory,
    IOptions<MarketplaceModuleOptions> options,
    InstalledPackageDetector installedPackageDetector,
    IMemoryCache cache
) : IMarketplaceContracts
{
    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task<MarketplaceSearchResult> SearchPackagesAsync(MarketplaceSearchRequest request)
    {
        var cacheKey =
            $"Marketplace:Search:{request.Query}:{request.Category}:{request.SortBy}:{request.Skip}:{request.Take}";

        var cached = await cache.GetOrCreateAsync(
            cacheKey,
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(
                    options.Value.SearchCacheDurationMinutes
                );
                return await FetchSearchResultsAsync(request);
            }
        );

        return cached ?? new MarketplaceSearchResult();
    }

    public async Task<MarketplacePackageDetail?> GetPackageDetailsAsync(string packageId)
    {
        var cacheKey = $"Marketplace:Detail:{packageId}";

        return await cache.GetOrCreateAsync(
            cacheKey,
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(
                    options.Value.DetailCacheDurationMinutes
                );
                return await FetchPackageDetailsAsync(packageId);
            }
        );
    }

    public Task<HashSet<string>> GetInstalledPackageIdsAsync() =>
        installedPackageDetector.GetInstalledPackageIdsAsync();

    private async Task<MarketplaceSearchResult> FetchSearchResultsAsync(
        MarketplaceSearchRequest request
    )
    {
        var client = httpClientFactory.CreateClient(MarketplaceConstants.ModuleName);
        var tag = options.Value.PackageTag;
        var query = string.IsNullOrWhiteSpace(request.Query)
            ? $"tag:{tag}"
            : $"tag:{tag} {request.Query}";

        var url =
            $"{options.Value.NuGetSearchBaseAddress}?q={Uri.EscapeDataString(query)}&skip={request.Skip}&take={request.Take}";

        var response = await client.GetFromJsonAsync<NuGetSearchResponse>(url, JsonOptions);
        if (response is null)
        {
            return new MarketplaceSearchResult();
        }

        var installedIds = await installedPackageDetector.GetInstalledPackageIdsAsync();

        var packages = response.Data.Select(d => MapToPackage(d, installedIds)).ToList();

        if (request.Category is not null and not MarketplaceCategory.All)
        {
            packages = packages.Where(p => p.Category == request.Category).ToList();
        }

        packages = request.SortBy switch
        {
            MarketplaceSortOption.Downloads =>
            [
                .. packages.OrderByDescending(p => p.TotalDownloads),
            ],
            MarketplaceSortOption.Alphabetical => [.. packages.OrderBy(p => p.Title)],
            MarketplaceSortOption.RecentlyUpdated => packages,
            _ => packages,
        };

        return new MarketplaceSearchResult { TotalHits = response.TotalHits, Packages = packages };
    }

    private async Task<MarketplacePackageDetail?> FetchPackageDetailsAsync(string packageId)
    {
        var client = httpClientFactory.CreateClient(MarketplaceConstants.ModuleName);
        var tag = options.Value.PackageTag;
        var searchAddress =
            $"{options.Value.NuGetSearchBaseAddress}?q=packageid:{Uri.EscapeDataString(packageId)} tag:{tag}&take=1";

        var searchResponse = await client.GetFromJsonAsync<NuGetSearchResponse>(
            searchAddress,
            JsonOptions
        );
        var packageData = searchResponse?.Data.FirstOrDefault();
        if (packageData is null)
        {
            return null;
        }

        var installedIds = await installedPackageDetector.GetInstalledPackageIdsAsync();

        return new MarketplacePackageDetail
        {
            Id = packageData.Id ?? string.Empty,
            Title = packageData.Title ?? packageData.Id ?? string.Empty,
            Description = packageData.Description ?? string.Empty,
            Authors = string.Join(", ", packageData.Authors ?? []),
            Icon = packageData.IconAddress ?? string.Empty,
            TotalDownloads = packageData.TotalDownloads,
            Tags = packageData.Tags ?? [],
            LatestVersion = packageData.Version ?? string.Empty,
            ProjectLink = packageData.ProjectAddress ?? string.Empty,
            LicenseLink = packageData.LicenseAddress ?? string.Empty,
            Category = CategoryMapper.MapCategory(packageData.Tags ?? []),
            IsInstalled = installedIds.Contains(packageData.Id ?? string.Empty),
            Versions = (packageData.Versions ?? [])
                .Select(v => new MarketplacePackageVersion
                {
                    Version = v.Version ?? string.Empty,
                    Downloads = v.Downloads,
                    Published = default,
                })
                .ToList(),
            Dependencies = [],
        };
    }

    private static MarketplacePackage MapToPackage(
        NuGetPackageData data,
        HashSet<string> installedIds
    )
    {
        return new MarketplacePackage
        {
            Id = data.Id ?? string.Empty,
            Title = data.Title ?? data.Id ?? string.Empty,
            Description = data.Description ?? string.Empty,
            Authors = string.Join(", ", data.Authors ?? []),
            Icon = data.IconAddress ?? string.Empty,
            TotalDownloads = data.TotalDownloads,
            Tags = data.Tags ?? [],
            LatestVersion = data.Version ?? string.Empty,
            ProjectLink = data.ProjectAddress ?? string.Empty,
            Category = CategoryMapper.MapCategory(data.Tags ?? []),
            IsInstalled = installedIds.Contains(data.Id ?? string.Empty),
        };
    }
}

// NuGet V3 Search API response models — instantiated by JSON deserialization
[JsonSerializable(typeof(NuGetSearchResponse))]
internal sealed partial class NuGetJsonContext : JsonSerializerContext;

internal sealed record NuGetSearchResponse
{
    [JsonPropertyName("totalHits")]
    public int TotalHits { get; init; }

    [JsonPropertyName("data")]
    public List<NuGetPackageData> Data { get; init; } = [];
}

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

internal sealed record NuGetVersionData
{
    [JsonPropertyName("version")]
    public string? Version { get; init; }

    [JsonPropertyName("downloads")]
    public long Downloads { get; init; }
}
