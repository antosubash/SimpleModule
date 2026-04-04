using SimpleModule.Core;

namespace SimpleModule.Marketplace.Contracts;

[Dto]
public class MarketplacePackage
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Authors { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public long TotalDownloads { get; set; }
    public List<string> Tags { get; set; } = [];
    public string LatestVersion { get; set; } = string.Empty;
    public string ProjectLink { get; set; } = string.Empty;
    public MarketplaceCategory Category { get; set; }
    public bool IsInstalled { get; set; }
    public bool IsVerified { get; set; }
}
