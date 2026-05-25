using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Keycloak.Contracts;

namespace SimpleModule.Keycloak.Endpoints;

public class KeycloakLogoutEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost(
                KeycloakModuleConstants.Routes.Logout,
                async (HttpContext context) =>
                {
                    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    await context.SignOutAsync(KeycloakModuleConstants.OidcSchemeName);
                }
            )
            .DisableAntiforgery();

        // GET for OIDC post-logout redirect
        app.MapGet("/keycloak/signout-callback", () => Results.Redirect("/")).AllowAnonymous();
    }
}
