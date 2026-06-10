using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SimpleModule.Core.Exceptions;

namespace SimpleModule.Core.Authorization.Policies;

public sealed class Authorizer(
    IServiceProvider serviceProvider,
    IOptions<PolicyAuthorizationOptions> options
) : IAuthorizer
{
    public async Task<AuthorizationResult> CheckAsync<TResource>(
        ClaimsPrincipal user,
        string action,
        TResource resource,
        CancellationToken cancellationToken = default
    )
    {
        // Fail-closed invariant: if the loop below never runs (no policy registered for
        // the resource type), this is a developer error and must throw — never allow.
        // The flag avoids copying the already-materialized GetServices array on a path
        // that runs once per guarded request.
        var evaluatedAnyPolicy = false;
        foreach (var policy in serviceProvider.GetServices<IPolicy<TResource>>())
        {
            evaluatedAnyPolicy = true;
            var result = await policy
                .AuthorizeAsync(user, action, resource, cancellationToken)
                .ConfigureAwait(false);
            if (!result.IsAllowed)
            {
                return result;
            }
        }

        if (!evaluatedAnyPolicy)
        {
            throw MissingPolicyException.ForResource(typeof(TResource));
        }

        return AuthorizationResult.Allow();
    }

    public async Task AuthorizeAsync<TResource>(
        ClaimsPrincipal user,
        string action,
        TResource resource,
        CancellationToken cancellationToken = default
    )
    {
        var result = await CheckAsync(user, action, resource, cancellationToken)
            .ConfigureAwait(false);
        if (result.IsAllowed)
        {
            return;
        }

        if (result.TreatAsNotFound || options.Value.NotFoundActions.Contains(action))
        {
            throw new NotFoundException();
        }

        throw result.Reason is null
            ? new ForbiddenException()
            : new ForbiddenException(result.Reason);
    }
}
