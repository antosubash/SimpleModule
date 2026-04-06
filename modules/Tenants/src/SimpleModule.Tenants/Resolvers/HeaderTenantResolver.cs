using Microsoft.AspNetCore.Http;
using SimpleModule.Tenants.Contracts;

namespace SimpleModule.Tenants.Resolvers;

internal static class HeaderTenantResolver
{
    public static string? Resolve(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(TenantsConstants.TenantIdHeader, out var values))
        {
            var value = values.ToString();
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        return null;
    }
}
