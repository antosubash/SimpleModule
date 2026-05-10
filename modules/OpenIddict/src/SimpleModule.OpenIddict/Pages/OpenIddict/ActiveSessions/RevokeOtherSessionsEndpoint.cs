using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Extensions;
using SimpleModule.OpenIddict.Contracts;

namespace SimpleModule.OpenIddict.Pages.OpenIddict.ActiveSessions;

public class RevokeOtherSessionsEndpoint : IEndpoint
{
    public const string Route = "/Identity/Account/Manage/ActiveSessions/revoke-others";
    public const string Method = "POST";

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost(
                Route,
                async Task<IResult> (
                    ClaimsPrincipal principal,
                    IOpenIddictSessionContracts sessionContracts
                ) =>
                {
                    var userId = principal.GetUserId();
                    if (string.IsNullOrEmpty(userId))
                    {
                        return TypedResults.Unauthorized();
                    }

                    var currentTokenId = ActiveSessionsHelpers.GetCurrentTokenId(principal);
                    await sessionContracts.RevokeOtherSessionsForUserAsync(userId, currentTokenId);
                    return TypedResults.Redirect("/Identity/Account/Manage/ActiveSessions");
                }
            )
            .RequireAuthorization()
            .DisableAntiforgery()
            .ExcludeFromDescription();
    }
}
