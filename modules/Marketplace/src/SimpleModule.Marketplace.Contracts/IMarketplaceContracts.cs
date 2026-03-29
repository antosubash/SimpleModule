namespace SimpleModule.Marketplace.Contracts;

public interface IMarketplaceContracts
{
    Task<MarketplaceSearchResult> SearchPackagesAsync(MarketplaceSearchRequest request);
    Task<MarketplacePackageDetail?> GetPackageDetailsAsync(string packageId);
    Task<HashSet<string>> GetInstalledPackageIdsAsync();
}
