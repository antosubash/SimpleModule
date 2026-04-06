using Microsoft.AspNetCore.Http;
using SimpleModule.Tenants.Contracts;

namespace SimpleModule.Tenants.Resolvers;

internal static class ClaimTenantResolver
{
    public static string? Resolve(HttpContext context)
    {
        var claim = context.User?.FindFirst(TenantsConstants.TenantClaimType);
        return claim?.Value;
    }
}
