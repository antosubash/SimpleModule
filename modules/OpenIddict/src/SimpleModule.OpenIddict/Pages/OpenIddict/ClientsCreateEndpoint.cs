using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.OpenIddict.Contracts;

namespace SimpleModule.OpenIddict.Pages.OpenIddict;

public class ClientsCreateEndpoint : IViewEndpoint
{
    public const string Route = OpenIddictModuleConstants.Routes.ClientsCreate;

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(Route, () => Inertia.Render("OpenIddict/OpenIddict/ClientsCreate", new { }))
            .RequireAuthorization(policy => policy.RequireRole("Admin"));
    }
}
