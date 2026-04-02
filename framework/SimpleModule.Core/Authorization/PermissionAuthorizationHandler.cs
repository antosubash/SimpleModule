using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace SimpleModule.Core.Authorization;

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement
    )
    {
        // Admin role bypasses all permission checks
        if (context.User.IsInRole("Admin"))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Exact match (fast path)
        if (context.User.HasClaim("permission", requirement.Permission))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Wildcard match: check all permission claims for wildcard patterns
        foreach (var claim in context.User.Claims)
        {
            if (
                claim.Type == "permission"
                && PermissionMatcher.IsMatch(claim.Value, requirement.Permission)
            )
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }
        }

        return Task.CompletedTask;
    }
}
