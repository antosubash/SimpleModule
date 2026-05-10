using SimpleModule.OpenIddict.Contracts;
using SimpleModule.Users.Contracts.Events;

namespace SimpleModule.OpenIddict.Services;

/// <summary>
/// When the Users module fires "Sign out everywhere", revoke all OpenIddict access and refresh
/// tokens for that user. Bearer/refresh-token holders bypass the cookie SecurityStampValidator,
/// so they need explicit revocation; Users avoids a hard reference to OpenIddict by going
/// through the event bus.
/// </summary>
public static class UserSignedOutEverywhereHandler
{
    public static Task Handle(
        UserSignedOutEverywhereEvent @event,
        IOpenIddictSessionContracts sessions,
        CancellationToken cancellationToken
    ) => sessions.RevokeAllSessionsForUserAsync(@event.UserId.Value, cancellationToken);
}
