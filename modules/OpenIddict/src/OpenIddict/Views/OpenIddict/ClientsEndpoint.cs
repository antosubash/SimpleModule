using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using OpenIddict.Abstractions;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;

namespace SimpleModule.OpenIddict.Views.OpenIddict;

public class ClientsEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/openiddict/clients",
                async (IOpenIddictApplicationManager manager) =>
                {
                    var clients = new List<object>();

                    await foreach (var application in manager.ListAsync())
                    {
                        clients.Add(
                            new
                            {
                                id = await manager.GetIdAsync(application),
                                clientId = await manager.GetClientIdAsync(application),
                                displayName = await manager.GetDisplayNameAsync(application),
                                clientType = await manager.GetClientTypeAsync(application),
                            }
                        );
                    }

                    return Inertia.Render("OpenIddict/OpenIddict/Clients", new { clients });
                }
            )
            .RequireAuthorization(policy => policy.RequireRole("Admin"));
    }
}
