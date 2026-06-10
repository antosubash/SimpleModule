using System.Security.Claims;

namespace SimpleModule.Core.Authorization.Policies;

/// <summary>
/// Encapsulates instance-level authorization rules for a resource type — ownership,
/// tenancy, state-machine rules. Layered on top of string permissions: keep
/// <c>.RequirePermission(...)</c> as the coarse capability gate and use a policy for
/// per-resource decisions. Implementations are auto-discovered by the source generator
/// and registered as scoped services; the resource type must be a contracts DTO (SM0058).
/// </summary>
public interface IPolicy<in TResource>
{
    Task<AuthorizationResult> AuthorizeAsync(
        ClaimsPrincipal user,
        string action,
        TResource resource,
        CancellationToken cancellationToken = default
    );
}
