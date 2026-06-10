using System.Security.Claims;

namespace SimpleModule.Core.Authorization.Policies;

/// <summary>
/// Dispatches instance-level authorization checks to the registered
/// <see cref="IPolicy{TResource}"/> implementations for the resource type.
/// </summary>
public interface IAuthorizer
{
    /// <summary>
    /// Runs the registered policies for <typeparamref name="TResource"/> in registration
    /// order, stopping at the first deny. A single deny wins; allow requires every policy
    /// to allow. Throws <see cref="MissingPolicyException"/> when no policy is registered.
    /// </summary>
    Task<AuthorizationResult> CheckAsync<TResource>(
        ClaimsPrincipal user,
        string action,
        TResource resource,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Like <see cref="CheckAsync{TResource}"/> but throws on deny:
    /// <see cref="Exceptions.NotFoundException"/> for actions listed in
    /// <see cref="PolicyAuthorizationOptions.NotFoundActions"/> (anti-enumeration),
    /// otherwise <see cref="Exceptions.ForbiddenException"/>.
    /// </summary>
    Task AuthorizeAsync<TResource>(
        ClaimsPrincipal user,
        string action,
        TResource resource,
        CancellationToken cancellationToken = default
    );
}
