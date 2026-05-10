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
                    // RequireAuthorization short-circuits unauthenticated requests
                    // before the handler runs, so userId is always present.
                    var userId = principal.GetUserId()!;
                    var currentTokenId = ActiveSessionsHelpers.GetCurrentTokenId(principal);

                    var result = await sessionContracts.TryRevokeSessionForUserAsync(
                        tokenId,
                        userId,
                        currentTokenId
                    );

                    return result switch
                    {
                        // Self-revoke is rejected with 400 — revoking the
                        // caller's own session would sign them out from under
                        // their own request.
                        RevokeSessionResult.BlockedCurrent => TypedResults.BadRequest(),
                        // 404 (not 403) when the token is missing or owned by
                        // someone else, so the response shape doesn't leak
                        // whether a token id exists for a different user.
                        RevokeSessionResult.NotFound => TypedResults.NotFound(),
                        _ => TypedResults.Redirect("/Identity/Account/Manage/ActiveSessions"),
                    };
                }
            )
            .RequireAuthorization()
            .DisableAntiforgery()
            .ExcludeFromDescription();
    }
}
