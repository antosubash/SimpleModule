using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using OpenIddict.Abstractions;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;

namespace SimpleModule.OpenIddict.Views;

[ViewPage("OpenIddict/OpenIddict/ClientsEdit")]
public class ClientsEditEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/clients/{id}/edit",
                async (string id, IOpenIddictApplicationManager manager, string? tab) =>
                {
                    var application = await manager.FindByIdAsync(id);
                    if (application is null)
                        return TypedResults.NotFound();

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

        app.MapPost(
                "/clients/{id}",
                async (
                    string id,
                    [FromForm] string? displayName,
                    [FromForm] string? clientType,
                    IOpenIddictApplicationManager manager
                ) =>
                {
                    var application = await manager.FindByIdAsync(id);
                    if (application is null)
                        return Results.NotFound();

                    var descriptor = new OpenIddictApplicationDescriptor();
                    await manager.PopulateAsync(descriptor, application);
                    descriptor.DisplayName = displayName;
                    descriptor.ClientType = clientType ?? descriptor.ClientType;
                    await manager.UpdateAsync(application, descriptor);
                    return TypedResults.Redirect($"/openiddict/clients/{id}/edit");
                }
            )
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .DisableAntiforgery();

        app.MapPost(
                "/clients/{id}/uris",
                async (string id, HttpContext context, IOpenIddictApplicationManager manager) =>
                {
                    var application = await manager.FindByIdAsync(id);
                    if (application is null)
                        return Results.NotFound();

                    var descriptor = new OpenIddictApplicationDescriptor();
                    await manager.PopulateAsync(descriptor, application);

                    descriptor.RedirectUris.Clear();
                    descriptor.PostLogoutRedirectUris.Clear();

                    var form = await context.Request.ReadFormAsync();

                    foreach (
                        var uri in form["redirectUris"].Where(u => !string.IsNullOrWhiteSpace(u))
                    )
                    {
                        descriptor.RedirectUris.Add(new Uri(uri!));
                    }

                    foreach (
                        var uri in form["postLogoutUris"].Where(u => !string.IsNullOrWhiteSpace(u))
                    )
                    {
                        descriptor.PostLogoutRedirectUris.Add(new Uri(uri!));
                    }

                    await manager.UpdateAsync(application, descriptor);

                    return TypedResults.Redirect($"/openiddict/clients/{id}/edit?tab=uris");
                }
            )
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .DisableAntiforgery();

        app.MapPost(
                "/clients/{id}/permissions",
                async (string id, HttpContext context, IOpenIddictApplicationManager manager) =>
                {
                    var application = await manager.FindByIdAsync(id);
                    if (application is null)
                        return Results.NotFound();

                    var descriptor = new OpenIddictApplicationDescriptor();
                    await manager.PopulateAsync(descriptor, application);

                    descriptor.Permissions.Clear();

                    var form = await context.Request.ReadFormAsync();

                    foreach (
                        var perm in form["permissions"].Where(p => !string.IsNullOrWhiteSpace(p))
                    )
                    {
                        descriptor.Permissions.Add(perm!);
                    }

                    await manager.UpdateAsync(application, descriptor);

                    return TypedResults.Redirect($"/openiddict/clients/{id}/edit?tab=permissions");
                }
            )
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .DisableAntiforgery();
    }
}
