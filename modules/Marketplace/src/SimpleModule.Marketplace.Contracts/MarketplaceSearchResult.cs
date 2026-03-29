using SimpleModule.Core;

namespace SimpleModule.Marketplace.Contracts;

[Dto]
public class MarketplaceSearchResult
{
    public int TotalHits { get; set; }
    public List<MarketplacePackage> Packages { get; set; } = [];
}
