namespace SimpleModule.Marketplace.Contracts;

public static class MarketplaceConstants
{
    public const string ModuleName = "Marketplace";
    public const string RoutePrefix = "/api/marketplace";
    public const string ViewPrefix = "/marketplace";

    public static class Routes
    {
        public const string Search = "/";
        public const string GetById = "/{id}";

        public const string Browse = "/";
        public const string Detail = "/{id}";
    }
}
