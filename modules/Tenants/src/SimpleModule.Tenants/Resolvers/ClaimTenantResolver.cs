using Microsoft.AspNetCore.Http;

namespace SimpleModule.Tenants.Resolvers;

internal static class ClaimTenantResolver
{
    public static string? Resolve(HttpContext context)
    {
        var claim = context.User?.FindFirst(TenantsConstants.TenantClaimType);
        return claim?.Value;
    }
}
