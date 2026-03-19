using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;

namespace SimpleModule.OpenIddict.Views.OpenIddict;

public class ClientsCreateEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/openiddict/clients/create",
                () => Inertia.Render("OpenIddictModule/OpenIddict/ClientsCreate", new { })
            )
            .RequireAuthorization(policy => policy.RequireRole("Admin"));
    }
}
