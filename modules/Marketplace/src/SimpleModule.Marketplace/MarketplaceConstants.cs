using SimpleModule.Marketplace.Contracts;

namespace SimpleModule.Marketplace;

public static class MarketplaceConstants
{
    public const string ModuleName = Contracts.MarketplaceConstants.ModuleName;
    public const string RoutePrefix = Contracts.MarketplaceConstants.RoutePrefix;
    public const string ViewPrefix = Contracts.MarketplaceConstants.ViewPrefix;

    public static class Routes
    {
        public const string Search = Contracts.MarketplaceConstants.Routes.Search;
        public const string GetById = Contracts.MarketplaceConstants.Routes.GetById;
        public const string Browse = Contracts.MarketplaceConstants.Routes.Browse;
        public const string Detail = Contracts.MarketplaceConstants.Routes.Detail;
    }
}
