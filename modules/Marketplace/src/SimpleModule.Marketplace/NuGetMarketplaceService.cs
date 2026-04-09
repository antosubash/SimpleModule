using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using SimpleModule.Core.Caching;
using SimpleModule.Marketplace.Contracts;

namespace SimpleModule.Marketplace;

public class NuGetMarketplaceService(
    IHttpClientFactory httpClientFactory,
    IOptions<MarketplaceModuleOptions> options,
    InstalledPackageDetector installedPackageDetector,
    ICacheStore cache
) : IMarketplaceContracts
{
    public async Task<MarketplaceSearchResult> SearchPackagesAsync(MarketplaceSearchRequest request)
    {
        var cacheKey = $"Marketplace:Search:{request.Query}";

        var cached = await cache.GetOrCreateAsync(
            cacheKey,
            async _ => await FetchAllPackagesAsync(request.Query),
            CacheEntryOptions.Expires(
                TimeSpan.FromMinutes(options.Value.SearchCacheDurationMinutes)
            )
        );

        var result = cached ?? new MarketplaceSearchResult();

        var packages = result.Packages;

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
            _ => packages,
        };

        var totalHits = packages.Count;
        packages = packages.Skip(request.Skip).Take(request.Take).ToList();

        return new MarketplaceSearchResult { TotalHits = totalHits, Packages = packages };
    }

    public async Task<MarketplacePackageDetail?> GetPackageDetailsAsync(string packageId)
    {
        var cacheKey = $"Marketplace:Detail:{packageId}";

        return await cache.GetOrCreateAsync(
            cacheKey,
            async _ => await FetchPackageDetailsAsync(packageId),
            CacheEntryOptions.Expires(
                TimeSpan.FromMinutes(options.Value.DetailCacheDurationMinutes)
            )
        );
    }

    public Task<HashSet<string>> GetInstalledPackageIdsAsync() =>
        installedPackageDetector.GetInstalledPackageIdsAsync();

    private async Task<MarketplaceSearchResult> FetchAllPackagesAsync(string? searchQuery)
    {
        try
        {
            var client = httpClientFactory.CreateClient(MarketplaceConstants.ModuleName);
            var verifiedAuthors = options.Value.VerifiedAuthors;

            var installedIdsTask = installedPackageDetector.GetInstalledPackageIdsAsync();
            var nugetTasks = verifiedAuthors.Select(author =>
                FetchPackagesForOwnerAsync(client, author, searchQuery)
            );
            var results = await Task.WhenAll(nugetTasks);
            var installedIds = await installedIdsTask;

            var packages = results
                .SelectMany(r => r)
                .DistinctBy(d => d.Id, StringComparer.OrdinalIgnoreCase)
                .Where(d =>
                    d.Id?.EndsWith(".Contracts", StringComparison.OrdinalIgnoreCase) != true
                )
                .Select(d => MapToPackage(d, installedIds, verifiedAuthors))
                .ToList();

            return new MarketplaceSearchResult { TotalHits = packages.Count, Packages = packages };
        }
        catch (HttpRequestException)
        {
            return new MarketplaceSearchResult();
        }
    }

    private async Task<List<NuGetPackageData>> FetchPackagesForOwnerAsync(
        HttpClient client,
        string owner,
        string? searchQuery
    )
    {
        var query = string.IsNullOrWhiteSpace(searchQuery)
            ? $"owner:{owner}"
            : $"owner:{owner} {searchQuery}";

        var url =
            $"{options.Value.NuGetSearchBaseAddress}?q={Uri.EscapeDataString(query)}&take=100";

        var response = await client.GetFromJsonAsync<NuGetSearchResponse>(url);
        return response?.Data ?? [];
    }

    private async Task<MarketplacePackageDetail?> FetchPackageDetailsAsync(string packageId)
    {
        try
        {
            var client = httpClientFactory.CreateClient(MarketplaceConstants.ModuleName);
            var verifiedAuthors = options.Value.VerifiedAuthors;

            var authorTasks = verifiedAuthors.Select(author =>
            {
                var query = $"packageid:{packageId} owner:{author}";
                var url =
                    $"{options.Value.NuGetSearchBaseAddress}?q={Uri.EscapeDataString(query)}&take=1";
                return client.GetFromJsonAsync<NuGetSearchResponse>(url);
            });
            var responses = await Task.WhenAll(authorTasks);
            var packageData = responses
                .Select(r => r?.Data.FirstOrDefault())
                .FirstOrDefault(d => d is not null);

            if (packageData is null)
            {
                return null;
            }

            var installedIdsTask = installedPackageDetector.GetInstalledPackageIdsAsync();
            var readmeTask = FetchReadmeAsync(client, packageData.Id, packageData.Version);
            await Task.WhenAll(installedIdsTask, readmeTask);

            var installedIds = await installedIdsTask;
            var basePackage = MapToPackage(packageData, installedIds, verifiedAuthors);
            var readme = await readmeTask;

            return new MarketplacePackageDetail
            {
                Id = basePackage.Id,
                Title = basePackage.Title,
                Description = basePackage.Description,
                Authors = basePackage.Authors,
                Icon = basePackage.Icon,
                TotalDownloads = basePackage.TotalDownloads,
                Tags = basePackage.Tags,
                LatestVersion = basePackage.LatestVersion,
                ProjectLink = basePackage.ProjectLink,
                Category = basePackage.Category,
                IsInstalled = basePackage.IsInstalled,
                IsVerified = basePackage.IsVerified,
                LicenseLink = packageData.LicenseAddress ?? string.Empty,
                Versions = (packageData.Versions ?? [])
                    .Select(v => new MarketplacePackageVersion
                    {
                        Version = v.Version ?? string.Empty,
                        Downloads = v.Downloads,
                    })
                    .ToList(),
                Dependencies = [],
                Readme = readme,
            };
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    [SuppressMessage(
        "Globalization",
        "CA1308:Normalize strings to uppercase",
        Justification = "NuGet flat container API requires lowercase package IDs"
    )]
    private async Task<string> FetchReadmeAsync(
        HttpClient client,
        string? packageId,
        string? version
    )
    {
        if (string.IsNullOrEmpty(packageId) || string.IsNullOrEmpty(version))
        {
            return string.Empty;
        }

        try
        {
            var id = packageId.ToLowerInvariant();
            var ver = version.ToLowerInvariant();
            var readmeUri = new Uri(
                $"{options.Value.NuGetFlatContainerBaseAddress}/{id}/{ver}/readme"
            );
            using var response = await client.GetAsync(readmeUri);

            if (!response.IsSuccessStatusCode)
            {
                return string.Empty;
            }

            return await response.Content.ReadAsStringAsync();
        }
        catch (HttpRequestException)
        {
            return string.Empty;
        }
    }

    private static MarketplacePackage MapToPackage(
        NuGetPackageData data,
        HashSet<string> installedIds,
        List<string> verifiedAuthors
    )
    {
        var authors = data.Authors ?? [];
        var isVerified = authors.Exists(a =>
            verifiedAuthors.Contains(a, StringComparer.OrdinalIgnoreCase)
        );

        return new MarketplacePackage
        {
            Id = data.Id ?? string.Empty,
            Title = data.Title ?? data.Id ?? string.Empty,
            Description = data.Description ?? string.Empty,
            Authors = string.Join(", ", authors),
            Icon = data.IconAddress ?? string.Empty,
            TotalDownloads = data.TotalDownloads,
            Tags = data.Tags ?? [],
            LatestVersion = data.Version ?? string.Empty,
            ProjectLink = data.ProjectAddress ?? string.Empty,
            Category = CategoryMapper.MapCategory(data.Tags ?? []),
            IsInstalled = installedIds.Contains(data.Id ?? string.Empty),
            IsVerified = isVerified,
        };
    }
}

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
