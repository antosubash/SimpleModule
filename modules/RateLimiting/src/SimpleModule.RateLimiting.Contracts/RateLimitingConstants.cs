namespace SimpleModule.RateLimiting.Contracts;

public static class RateLimitingConstants
{
    public const string ModuleName = "RateLimiting";
    public const string RoutePrefix = "/api/rate-limiting";
    public const string ViewPrefix = "/rate-limiting";

    public static class Routes
    {
        // API endpoints
        public const string GetAll = "/";
        public const string Create = "/";
        public const string GetById = "/{id:int}";
        public const string Update = "/{id:int}";
        public const string Delete = "/{id:int}";
        public const string GetActivePolicies = "/active";

        // View endpoints
        public const string Admin = "/";
    }
}
