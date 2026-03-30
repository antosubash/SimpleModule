namespace SimpleModule.Tenants;

public static class TenantsConstants
{
    public const string ModuleName = "Tenants";
    public const string RoutePrefix = "/api/tenants";
    public const string TenantIdHeader = "X-Tenant-Id";
    public const string TenantClaimType = "tenant_id";
}
