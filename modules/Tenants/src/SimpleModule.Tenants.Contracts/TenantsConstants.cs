namespace SimpleModule.Tenants.Contracts;

public static class TenantsConstants
{
    public const string ModuleName = "Tenants";
    public const string RoutePrefix = "/api/tenants";
    public const string ViewPrefix = "/tenants";
    public const string TenantIdHeader = "X-Tenant-Id";
    public const string TenantClaimType = "tenant_id";

    public static class Routes
    {
        public static class Api
        {
            public const string GetAll = "/";
            public const string Create = "/";
            public const string GetById = "/{id}";
            public const string Update = "/{id}";
            public const string Delete = "/{id}";
            public const string ChangeStatus = "/{id}/status";
            public const string AddHost = "/{id}/hosts";
            public const string RemoveHost = "/{id}/hosts/{hostId}";
            public const string GetTenantFeatures = "/{id}/features";
            public const string SetTenantFeature = "/{id}/features/{flagName}";
            public const string DeleteTenantFeature = "/{id}/features/{flagName}";
        }

        public static class Views
        {
            public const string Browse = "/browse";
            public const string Manage = "/manage";
            public const string Create = "/create";
            public const string Edit = "/{id}/edit";
            public const string Features = "/{id}/features";
        }
    }
}
