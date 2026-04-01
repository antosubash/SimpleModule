using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.OpenIddict.Contracts;

namespace SimpleModule.Admin.Endpoints.Admin;

public class AdminSessionsEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/admin/users/{id}/sessions")
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .DisableAntiforgery();

        // DELETE /admin/users/{id}/sessions/{tokenId} — Revoke individual session
        group.MapDelete(
            "/{tokenId}",
            async Task<IResult> (
                string id,
                string tokenId,
                IOpenIddictSessionContracts sessionContracts
            ) =>
            {
                await sessionContracts.RevokeSessionAsync(tokenId);

                return TypedResults.Redirect($"/admin/users/{id}/edit?tab=sessions");
            }
        );

        // DELETE /admin/users/{id}/sessions — Revoke all sessions
        group.MapDelete(
            "/",
            async Task<IResult> (string id, IOpenIddictSessionContracts sessionContracts) =>
            {
                await sessionContracts.RevokeAllSessionsForUserAsync(id);

                return TypedResults.Redirect($"/admin/users/{id}/edit?tab=sessions");
            }
        );
    }
}
