using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using OpenIddict.Abstractions;
using SimpleModule.Core;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace SimpleModule.OpenIddict.Endpoints.OpenIddict;

public class ClientsActionEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/openiddict/clients")
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .DisableAntiforgery();

        // POST / — Create client
        group.MapPost(
            "/",
            async (
                [FromForm] string clientId,
                [FromForm] string displayName,
                [FromForm] string clientType,
                [FromForm] string? clientSecret,
                HttpContext context,
                IOpenIddictApplicationManager manager
            ) =>
            {
                var descriptor = new OpenIddictApplicationDescriptor
                {
                    ClientId = clientId.Trim(),
                    DisplayName = displayName.Trim(),
                    ClientType = clientType,
                };

                if (
                    clientType == ClientTypes.Confidential
                    && !string.IsNullOrWhiteSpace(clientSecret)
                )
                {
                    descriptor.ClientSecret = clientSecret;
                }

                var form = await context.Request.ReadFormAsync();
                foreach (var uri in form["redirectUris"].Where(u => !string.IsNullOrWhiteSpace(u)))
                {
                    descriptor.RedirectUris.Add(new Uri(uri!));
                }

                foreach (
                    var uri in form["postLogoutUris"].Where(u => !string.IsNullOrWhiteSpace(u))
                )
                {
                    descriptor.PostLogoutRedirectUris.Add(new Uri(uri!));
                }

                foreach (var perm in form["permissions"].Where(p => !string.IsNullOrWhiteSpace(p)))
                {
                    descriptor.Permissions.Add(perm!);
                }

                var application = await manager.CreateAsync(descriptor);
                var id = await manager.GetIdAsync(application);

                return Results.Redirect($"/openiddict/clients/{id}/edit");
            }
        );

        // POST /{id} — Update client details
        group.MapPost(
            "/{id}",
            async (
                string id,
                [FromForm] string displayName,
                [FromForm] string clientType,
                IOpenIddictApplicationManager manager
            ) =>
            {
                var application = await manager.FindByIdAsync(id);
                if (application is null)
                    return Results.NotFound();

                var descriptor = new OpenIddictApplicationDescriptor();
                await manager.PopulateAsync(descriptor, application);

                descriptor.DisplayName = displayName.Trim();
                descriptor.ClientType = clientType;

                await manager.UpdateAsync(application, descriptor);

                return Results.Redirect($"/openiddict/clients/{id}/edit?tab=details");
            }
        );

        // POST /{id}/uris — Update redirect URIs
        group.MapPost(
            "/{id}/uris",
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
                foreach (var uri in form["redirectUris"].Where(u => !string.IsNullOrWhiteSpace(u)))
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

                return Results.Redirect($"/openiddict/clients/{id}/edit?tab=uris");
            }
        );

        // POST /{id}/permissions — Update permissions
        group.MapPost(
            "/{id}/permissions",
            async (string id, HttpContext context, IOpenIddictApplicationManager manager) =>
            {
                var application = await manager.FindByIdAsync(id);
                if (application is null)
                    return Results.NotFound();

                var descriptor = new OpenIddictApplicationDescriptor();
                await manager.PopulateAsync(descriptor, application);

                descriptor.Permissions.Clear();

                var form = await context.Request.ReadFormAsync();
                foreach (var perm in form["permissions"].Where(p => !string.IsNullOrWhiteSpace(p)))
                {
                    descriptor.Permissions.Add(perm!);
                }

                await manager.UpdateAsync(application, descriptor);

                return Results.Redirect($"/openiddict/clients/{id}/edit?tab=permissions");
            }
        );

        // DELETE /{id} — Delete client
        group.MapDelete(
            "/{id}",
            async (string id, IOpenIddictApplicationManager manager) =>
            {
                var application = await manager.FindByIdAsync(id);
                if (application is null)
                    return Results.NotFound();

                await manager.DeleteAsync(application);

                return Results.Redirect("/openiddict/clients");
            }
        );
    }
}
