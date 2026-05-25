using SimpleModule.Identity.Contracts;
using SimpleModule.Users.Contracts.Events;

namespace SimpleModule.Keycloak.Handlers;

/// <summary>
/// When the Users module fires "Sign out everywhere", revoke all Keycloak sessions
/// for that user. This mirrors the OpenIddict handler pattern — bearer/refresh-token
/// holders bypass the cookie SecurityStampValidator, so they need explicit revocation
/// via the event bus.
/// </summary>
public static class UserSignedOutEverywhereHandler
{
    public static async Task Handle(
        UserSignedOutEverywhereEvent message,
        ISessionContracts sessionContracts
    )
    {
        await sessionContracts.RevokeAllSessionsForUserAsync(message.UserId.Value);
    }
}
