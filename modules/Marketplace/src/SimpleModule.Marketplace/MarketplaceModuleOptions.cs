using SimpleModule.Core;

namespace SimpleModule.Marketplace;

public class MarketplaceModuleOptions : IModuleOptions
{
    public string NuGetSearchBaseAddress { get; set; } = "https://api.nuget.org/v3/search";
    public string NuGetRegistrationBaseAddress { get; set; } =
        "https://api.nuget.org/v3/registration5-gz-semver2";
    public string PackageTag { get; set; } = "simplemodule";
    public int SearchCacheDurationMinutes { get; set; } = 5;
    public int DetailCacheDurationMinutes { get; set; } = 10;
}
