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
        var anyPolicy = false;
        foreach (var policy in serviceProvider.GetServices<IPolicy<TResource>>())
        {
            anyPolicy = true;
            var result = await policy
                .AuthorizeAsync(user, action, resource, cancellationToken)
                .ConfigureAwait(false);
            if (!result.IsAllowed)
            {
                return result;
            }
        }

        if (!anyPolicy)
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

        if (options.Value.NotFoundActions.Contains(action))
        {
            throw new NotFoundException();
        }

        throw result.Reason is null
            ? new ForbiddenException()
            : new ForbiddenException(result.Reason);
    }
}
