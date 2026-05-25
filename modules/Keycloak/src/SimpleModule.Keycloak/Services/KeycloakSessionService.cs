using SimpleModule.Identity.Contracts;

namespace SimpleModule.Keycloak.Services;

/// <summary>
/// Implements <see cref="ISessionContracts"/> by delegating to the Keycloak
/// Admin REST API via <see cref="KeycloakAdminClient"/>.
/// </summary>
public sealed class KeycloakSessionService(KeycloakAdminClient adminClient) : ISessionContracts
{
    public async Task<IReadOnlyList<SessionDto>> GetActiveSessionsForUserAsync(
        string userId,
        CancellationToken cancellationToken = default
    )
    {
        var sessions = await adminClient.GetUserSessionsAsync(userId, cancellationToken);
        return sessions.Select(s => ToSessionDto(s, currentSessionId: null)).ToList();
    }

    public async Task<IReadOnlyList<SessionDto>> GetActiveSessionsForUserAsync(
        string userId,
        string? currentTokenId,
        CancellationToken cancellationToken = default
    )
    {
        var sessions = await adminClient.GetUserSessionsAsync(userId, cancellationToken);
        return sessions.Select(s => ToSessionDto(s, currentTokenId)).ToList();
    }

    public async Task<RevokeSessionResult> TryRevokeSessionForUserAsync(
        string tokenId,
        string userId,
        string? currentTokenId,
        CancellationToken cancellationToken = default
    )
    {
        // In Keycloak, the tokenId maps to the session ID. Guard against
        // revoking the caller's own session.
        if (
            !string.IsNullOrEmpty(currentTokenId)
            && string.Equals(tokenId, currentTokenId, StringComparison.Ordinal)
        )
        {
            return RevokeSessionResult.BlockedCurrent;
        }

        // Verify the session belongs to this user before revoking.
        var sessions = await adminClient.GetUserSessionsAsync(userId, cancellationToken);
        var target = sessions.FirstOrDefault(s =>
            string.Equals(s.Id, tokenId, StringComparison.Ordinal)
        );

        if (target is null)
            return RevokeSessionResult.NotFound;

        var deleted = await adminClient.DeleteSessionAsync(tokenId, cancellationToken);
        return deleted ? RevokeSessionResult.Revoked : RevokeSessionResult.NotFound;
    }

    public async Task RevokeSessionAsync(
        string tokenId,
        CancellationToken cancellationToken = default
    )
    {
        await adminClient.DeleteSessionAsync(tokenId, cancellationToken);
    }

    public async Task RevokeAllSessionsForUserAsync(
        string userId,
        CancellationToken cancellationToken = default
    )
    {
        await adminClient.LogoutUserAsync(userId, cancellationToken);
    }

    public async Task RevokeOtherSessionsForUserAsync(
        string userId,
        string? currentTokenId,
        CancellationToken cancellationToken = default
    )
    {
        var sessions = await adminClient.GetUserSessionsAsync(userId, cancellationToken);

        foreach (var session in sessions)
        {
            // Skip the current session.
            if (
                !string.IsNullOrEmpty(currentTokenId)
                && string.Equals(session.Id, currentTokenId, StringComparison.Ordinal)
            )
            {
                continue;
            }

            await adminClient.DeleteSessionAsync(session.Id, cancellationToken);
        }
    }

    private static SessionDto ToSessionDto(KeycloakSessionDto session, string? currentSessionId)
    {
        DateTimeOffset? creationDate = session.Start.HasValue
            ? DateTimeOffset.FromUnixTimeMilliseconds(session.Start.Value)
            : null;

        // Derive a display name from the first client in the session, if available.
        string? applicationName = session.Clients?.Values.FirstOrDefault();

        return new SessionDto
        {
            TokenId = session.Id,
            Type = "keycloak_session",
            ApplicationName = applicationName,
            CreationDate = creationDate,
            // Keycloak sessions don't have an explicit per-session expiration in the
            // admin API response — the session lifetime is governed by realm/client
            // timeouts. Set to null; the UI should handle null gracefully.
            ExpirationDate = null,
            IsCurrent =
                !string.IsNullOrEmpty(currentSessionId)
                && string.Equals(session.Id, currentSessionId, StringComparison.Ordinal),
        };
    }
}
