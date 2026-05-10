using System.Security.Claims;

namespace SimpleModule.OpenIddict.Pages.OpenIddict.ActiveSessions;

internal static class ActiveSessionsHelpers
{
    // OpenIddict's validation handler exposes the originating token id on the
    // principal as a private claim. For cookie-authenticated requests the claim
    // is absent, which is fine — no OpenIddict session will match the browser
    // cookie and every listed session will remain revocable.
    private const string AccessTokenIdClaim = "oi_tkn_id";
    private const string RefreshTokenIdClaim = "oi_reft_id";

    public static string? GetCurrentTokenId(ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(AccessTokenIdClaim)
            ?? principal.FindFirstValue(RefreshTokenIdClaim);
    }
}
