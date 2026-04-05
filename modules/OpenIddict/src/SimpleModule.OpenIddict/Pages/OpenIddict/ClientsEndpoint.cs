using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using OpenIddict.Abstractions;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.OpenIddict.Contracts;

namespace SimpleModule.OpenIddict.Pages.OpenIddict;

public class ClientsEndpoint : IViewEndpoint
{
    public const string Route = OpenIddictModuleConstants.Routes.Clients;

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                Route,
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

        app.MapPost(
                Route,
                async (
                    [FromForm] string clientId,
                    [FromForm] string? displayName,
                    [FromForm] string? clientType,
                    [FromForm] string? clientSecret,
                    IOpenIddictApplicationManager manager
                ) =>
                {
                    var descriptor = new OpenIddictApplicationDescriptor
                    {
                        ClientId = clientId,
                        DisplayName = displayName ?? clientId,
                        ClientType = clientType ?? OpenIddictConstants.ClientTypes.Public,
                    };

                    if (
                        clientType == OpenIddictConstants.ClientTypes.Confidential
                        && !string.IsNullOrEmpty(clientSecret)
                    )
                    {
                        descriptor.ClientSecret = clientSecret;
                    }

                    var application = await manager.CreateAsync(descriptor);
                    var id = await manager.GetIdAsync(application);
                    return TypedResults.Redirect($"/openiddict/clients/{id}/edit");
                }
            )
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .DisableAntiforgery();

        app.MapDelete(
                "/clients/{id}",
                async (string id, IOpenIddictApplicationManager manager) =>
                {
                    var application = await manager.FindByIdAsync(id);
                    if (application is null)
                    {
                        return Results.NotFound();
                    }

                    await manager.DeleteAsync(application);
                    return Results.Ok();
                }
            )
            .RequireAuthorization(policy => policy.RequireRole("Admin"));
    }
}
