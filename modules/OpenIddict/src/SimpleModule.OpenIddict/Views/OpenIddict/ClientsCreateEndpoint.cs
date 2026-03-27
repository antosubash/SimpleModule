using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;

namespace SimpleModule.OpenIddict.Views.OpenIddict;

[ViewPage("OpenIddict/OpenIddict/ClientsCreate")]
public class ClientsCreateEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/clients/create",
                () => Inertia.Render("OpenIddict/OpenIddict/ClientsCreate", new { })
            )
            .RequireAuthorization(policy => policy.RequireRole("Admin"));
    }
}
