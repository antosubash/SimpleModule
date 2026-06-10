using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core.Exceptions;

namespace SimpleModule.Core.Authorization.Policies;

public static class EndpointPolicyExtensions
{
    /// <summary>
    /// Declarative policy check: before the handler runs, loads the resource via the
    /// module's <see cref="IResourceResolver{TResource}"/> using the
    /// <paramref name="routeParameterName"/> route value, then authorizes
    /// <paramref name="action"/> against the registered <see cref="IPolicy{TResource}"/>.
    /// Missing resource surfaces 404; denial surfaces 403 (or 404 per
    /// <see cref="PolicyAuthorizationOptions.NotFoundActions"/>).
    /// </summary>
    public static RouteHandlerBuilder AuthorizeResource<TResource>(
        this RouteHandlerBuilder builder,
        string action,
        string routeParameterName = "id"
    )
    {
        return builder.AddEndpointFilter(async (context, next) =>
        {
            var httpContext = context.HttpContext;
            // A missing route value is static misconfiguration (the parameter either
            // exists in the route template or never does) — fail loudly, don't 404.
            var routeValue = httpContext.GetRouteValue(routeParameterName)?.ToString();
            if (string.IsNullOrEmpty(routeValue))
            {
                throw new InvalidOperationException(
                    $"AuthorizeResource<{typeof(TResource).Name}> found no '{routeParameterName}' "
                        + "route value. Pass the route parameter name used in the endpoint's "
                        + "route template (default is \"id\")."
                );
            }

            var resolver = httpContext.RequestServices.GetService<IResourceResolver<TResource>>();
            if (resolver is null)
            {
                throw new InvalidOperationException(
                    $"No IResourceResolver<{typeof(TResource).Name}> is registered. "
                        + "Register one in the owning module's ConfigureServices to use "
                        + "AuthorizeResource, or perform the check imperatively via IAuthorizer."
                );
            }

            var resource = await resolver.ResolveAsync(routeValue, httpContext.RequestAborted);
            if (resource is null)
            {
                throw new NotFoundException();
            }

            var authorizer = httpContext.RequestServices.GetRequiredService<IAuthorizer>();
            await authorizer.AuthorizeAsync(
                httpContext.User,
                action,
                resource,
                httpContext.RequestAborted
            );

            return await next(context);
        });
    }
}
