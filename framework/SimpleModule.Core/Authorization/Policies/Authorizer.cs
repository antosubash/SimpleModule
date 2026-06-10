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
        // Fail closed first: a resource type without any policy is a developer error.
        var policies = serviceProvider.GetServices<IPolicy<TResource>>().ToList();
        if (policies.Count == 0)
        {
            throw MissingPolicyException.ForResource(typeof(TResource));
        }

        foreach (var policy in policies)
        {
            var result = await policy
                .AuthorizeAsync(user, action, resource, cancellationToken)
                .ConfigureAwait(false);
            if (!result.IsAllowed)
            {
                return result;
            }
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
