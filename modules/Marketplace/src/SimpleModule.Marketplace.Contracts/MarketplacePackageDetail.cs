using SimpleModule.Core;

namespace SimpleModule.Marketplace.Contracts;

[Dto]
public class MarketplacePackageDetail : MarketplacePackage
{
    public string LicenseLink { get; set; } = string.Empty;
    public List<MarketplacePackageVersion> Versions { get; set; } = [];
    public List<string> Dependencies { get; set; } = [];
}

[Dto]
public class MarketplacePackageVersion
{
    public string Version { get; set; } = string.Empty;
    public long Downloads { get; set; }
}
