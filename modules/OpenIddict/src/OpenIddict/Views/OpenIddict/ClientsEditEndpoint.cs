using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using OpenIddict.Abstractions;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;

namespace SimpleModule.OpenIddict.Views.OpenIddict;

public class ClientsEditEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/openiddict/clients/{id}/edit",
                async (string id, IOpenIddictApplicationManager manager, string? tab) =>
                {
                    var application = await manager.FindByIdAsync(id);
                    if (application is null)
                        return Results.NotFound();

                    var descriptor = new OpenIddictApplicationDescriptor();
                    await manager.PopulateAsync(descriptor, application);

                    return Inertia.Render(
                        "OpenIddict/OpenIddict/ClientsEdit",
                        new
                        {
                            client = new
                            {
                                id = await manager.GetIdAsync(application),
                                clientId = await manager.GetClientIdAsync(application),
                                displayName = await manager.GetDisplayNameAsync(application),
                                clientType = await manager.GetClientTypeAsync(application),
                            },
                            redirectUris = descriptor
                                .RedirectUris.Select(u => u.ToString())
                                .ToList(),
                            postLogoutUris = descriptor
                                .PostLogoutRedirectUris.Select(u => u.ToString())
                                .ToList(),
                            permissions = descriptor.Permissions.ToList(),
                            tab = tab ?? "details",
                        }
                    );
                }
            )
            .RequireAuthorization(policy => policy.RequireRole("Admin"));
    }
}
