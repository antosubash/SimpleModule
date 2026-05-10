using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Extensions;
using SimpleModule.OpenIddict.Contracts;

namespace SimpleModule.OpenIddict.Pages.OpenIddict.ActiveSessions;

public class RevokeSessionEndpoint : IEndpoint
{
    public const string Route = "/Identity/Account/Manage/ActiveSessions/{tokenId}/revoke";
    public const string Method = "POST";

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost(
                Route,
                async Task<IResult> (
                    string tokenId,
                    ClaimsPrincipal principal,
                    IOpenIddictSessionContracts sessionContracts
                ) =>
                {
                    var userId = principal.GetUserId();
                    if (string.IsNullOrEmpty(userId))
                    {
                        return TypedResults.Unauthorized();
                    }

                    // Refuse to revoke the request's own session before touching
                    // the store, so a self-revoke can never silently sign the
                    // caller out from under their own request.
                    var currentTokenId = ActiveSessionsHelpers.GetCurrentTokenId(principal);
                    if (
                        !string.IsNullOrEmpty(currentTokenId)
                        && string.Equals(tokenId, currentTokenId, StringComparison.Ordinal)
                    )
                    {
                        return TypedResults.BadRequest();
                    }

                    // 404 (not 403) when the token is missing or owned by someone
                    // else, so the response shape doesn't leak whether a token id
                    // exists for a different user.
                    var revoked = await sessionContracts.TryRevokeSessionForUserAsync(
                        tokenId,
                        userId
                    );
                    if (!revoked)
                    {
                        return TypedResults.NotFound();
                    }

                    return TypedResults.Redirect("/Identity/Account/Manage/ActiveSessions");
                }
            )
            .RequireAuthorization()
            .DisableAntiforgery()
            .ExcludeFromDescription();
    }
}
