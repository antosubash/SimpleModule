using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.OpenIddict.Contracts;

namespace SimpleModule.OpenIddict.Endpoints.Connect;

[AllowAnonymous]
public class OAuthCallbackEndpoint : IEndpoint
{
    public const string Route = OpenIddictModuleConstants.Routes.OAuthCallback;

    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(Route, () => Inertia.Render("OpenIddict/OAuthCallback"));
}
