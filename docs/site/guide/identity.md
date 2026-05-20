---
outline: deep
---

# Identity & Sessions

The Users module owns the local identity store; the OpenIddict module owns issued tokens. This page covers the user-facing flows that span them: account lockout recovery, phone verification, active-session management, and global sign-out.

## Account lockout and self-service unlock

When ASP.NET Identity locks an account after repeated failed logins the user is redirected to `/Identity/Account/Lockout`. From there `Send unlock email` posts to `/Identity/Account/SendUnlockEmail`, which:

1. Resolves the user by email (silently no-ops on miss to avoid enumeration).
2. Generates a single-use token bound to the user and the `AccountUnlock` purpose.
3. Calls `IAccountUnlockEmailSender.SendUnlockLinkAsync(email, unlockLink)`.

Clicking the link lands on `/Identity/Account/UnlockAccount`, which validates the token, calls `userManager.SetLockoutEndDateAsync(...)` to clear the lockout, and signs the user out so they re-enter credentials.

`IAccountUnlockEmailSender` defaults to `ConsoleAccountUnlockEmailSender` (logs the link). Replace it with a production implementation that hands off to your transactional mail provider:

```csharp
public sealed class MailgunAccountUnlockEmailSender(IMailgunClient client) : IAccountUnlockEmailSender
{
    public Task SendUnlockLinkAsync(string email, string unlockLink) =>
        client.SendAsync(to: email, subject: "Unlock your account", html: Templates.Unlock(unlockLink));
}
```

Register the replacement in `Program.cs` after `AddSimpleModuleInfrastructure()`:

```csharp
builder.Services.AddScoped<IAccountUnlockEmailSender, MailgunAccountUnlockEmailSender>();
```

## Phone number confirmation

The account manage page collects an unconfirmed phone number and offers `Send code`. That action posts to `/Identity/Account/Manage/SendPhoneVerificationCode`, which uses `userManager.GenerateChangePhoneNumberTokenAsync(...)` and dispatches via `ISmsSender`:

```csharp
public interface ISmsSender
{
    Task SendVerificationCodeAsync(
        ApplicationUser user,
        string phoneNumber,
        string code,
        CancellationToken cancellationToken = default);
}
```

Provide your own implementation (Twilio, Vonage, AWS SNS) and register it the same way as the unlock sender. The default `ConsoleSmsSender` writes the code to logs for local development.

`/Identity/Account/Manage/ConfirmPhoneNumber` verifies the code with `userManager.ChangePhoneNumberAsync(...)`, which sets both the number and `PhoneNumberConfirmed = true`. `/Identity/Account/Manage/RemovePhoneNumber` clears both fields.

## Active sessions

Every refresh token issued by OpenIddict represents a live session. The manage page at `/Identity/Account/Manage` lists them so a user can audit and revoke individual logins without changing their password.

Sessions are exposed via `IOpenIddictSessionContracts`. The session-grouped overload collapses access + refresh tokens that share an `AuthorizationId` into a single row, so a user can't accidentally revoke half of their own login:

```csharp
public interface IOpenIddictSessionContracts
{
    Task<IReadOnlyList<UserSessionDto>> GetActiveSessionsForUserAsync(
        string userId,
        string? currentTokenId,
        CancellationToken cancellationToken = default);

    Task<RevokeSessionResult> TryRevokeSessionForUserAsync(
        string tokenId,
        string userId,
        string? currentTokenId,
        CancellationToken cancellationToken = default);

    Task RevokeAllSessionsForUserAsync(string userId, CancellationToken cancellationToken = default);

    Task RevokeOtherSessionsForUserAsync(
        string userId,
        string? currentTokenId,
        CancellationToken cancellationToken = default);
}
```

`UserSessionDto` carries `TokenId`, `Type`, `ApplicationName`, `CreationDate`, `ExpirationDate`, and an `IsCurrent` flag set when the row belongs to the request's own session.

`TryRevokeSessionForUserAsync` returns `RevokeSessionResult.NotFound` (404) for unknown or cross-user tokens — the endpoint deliberately does not distinguish "doesn't exist" from "belongs to someone else" — and `BlockedCurrent` (400) when the caller tries to revoke their own session, which would log them out mid-request.

## Sign out everywhere

`/Identity/Account/Manage/SignOutEverywhere` calls `RevokeOtherSessionsForUserAsync` (passing the current token id) and then bumps the user's security stamp via `userManager.UpdateSecurityStampAsync(...)`. The stamp change invalidates every cookie auth ticket issued before the bump, so even browser sessions held outside the OAuth flow are forced through re-authentication.

For credential-compromise flows, combine `RevokeAllSessionsForUserAsync` with `UpdateSecurityStampAsync` so even cookie-based sessions issued before the stamp bump are invalidated.

## Next Steps

- [Permissions](/guide/permissions) — claims-based authorization layered on top of identity.
- [Notifications](/guide/notifications) — channel the unlock and verification messages through a unified pipeline.
- [Settings](/guide/settings) — toggle lockout policy thresholds without redeploying.
