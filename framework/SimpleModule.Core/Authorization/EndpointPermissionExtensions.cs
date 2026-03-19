using Microsoft.AspNetCore.Builder;

namespace SimpleModule.Core.Authorization;

public static class EndpointPermissionExtensions
{
    public static TBuilder RequirePermission<TBuilder>(
        this TBuilder builder,
        params string[] permissions
    )
        where TBuilder : IEndpointConventionBuilder
    {
        return builder.RequireAuthorization(policy =>
        {
            policy.RequireAuthenticatedUser();
            foreach (var permission in permissions)
            {
                policy.AddRequirements(new PermissionRequirement(permission));
            }
        });
    }
}
