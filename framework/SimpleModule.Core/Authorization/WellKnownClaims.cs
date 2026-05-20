namespace SimpleModule.Core.Authorization;

public static class WellKnownClaims
{
    public const string Permission = "permission";

    /// <summary>Tenant identifier carried on the principal in multi-tenant deployments.</summary>
    public const string TenantId = "tenantid";
}
