using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Keycloak.Contracts;

namespace SimpleModule.Keycloak.Endpoints;

public class KeycloakLoginEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                KeycloakModuleConstants.Routes.Login,
                (HttpContext context, string? returnUrl) =>
                {
                    var redirectUri = returnUrl ?? "/";
                    return Results.Challenge(
                        new AuthenticationProperties { RedirectUri = redirectUri },
                        [KeycloakModuleConstants.OidcSchemeName]
                    );
                }
            )
            .AllowAnonymous();
    }
}
