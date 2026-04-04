using SimpleModule.Core;

namespace SimpleModule.Marketplace;

public class MarketplaceModuleOptions : IModuleOptions
{
    public string NuGetSearchBaseAddress { get; set; } = "https://azuresearch-usnc.nuget.org/query";
    public string NuGetRegistrationBaseAddress { get; set; } =
        "https://api.nuget.org/v3/registration5-gz-semver2";
    public string NuGetFlatContainerBaseAddress { get; set; } =
        "https://api.nuget.org/v3-flatcontainer";
    public List<string> VerifiedAuthors { get; set; } = ["antosubash"];
    public int SearchCacheDurationMinutes { get; set; } = 5;
    public int DetailCacheDurationMinutes { get; set; } = 10;
}
