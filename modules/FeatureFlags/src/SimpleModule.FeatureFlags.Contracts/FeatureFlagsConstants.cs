namespace SimpleModule.FeatureFlags.Contracts;

public static class FeatureFlagsConstants
{
    public const string ModuleName = "FeatureFlags";
    public const string RoutePrefix = "/api/feature-flags";
    public const string ViewPrefix = "/feature-flags";

    public static class Routes
    {
        // API endpoints
        public const string GetAll = "/";
        public const string Check = "/check/{name}";
        public const string GetOverrides = "/{name}/overrides";
        public const string SetOverride = "/{name}/overrides";
        public const string Update = "/{name}";
        public const string DeleteOverride = "/overrides/{id:int}";

        // View endpoints
        public const string Manage = "/manage";
    }
}
