namespace SimpleModule.OpenIddict.Contracts;

public interface IOpenIddictSessionContracts
{
    Task<IReadOnlyList<UserSessionDto>> GetActiveSessionsForUserAsync(
        string userId,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<UserSessionDto>> GetActiveSessionsForUserAsync(
        string userId,
        string? currentTokenId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Revokes the token if and only if its subject equals <paramref name="userId"/>.
    /// Returns true on revoke, false if the token does not exist or belongs to a
    /// different user. Single round-trip — used by the user-facing revoke endpoint
    /// to defend against cross-user token-id guessing without a separate ownership
    /// query.
    /// </summary>
    Task<bool> TryRevokeSessionForUserAsync(
        string tokenId,
        string userId,
        CancellationToken cancellationToken = default
    );

    Task RevokeSessionAsync(string tokenId, CancellationToken cancellationToken = default);

    Task RevokeAllSessionsForUserAsync(
        string userId,
        CancellationToken cancellationToken = default
    );

    Task RevokeOtherSessionsForUserAsync(
        string userId,
        string? currentTokenId,
        CancellationToken cancellationToken = default
    );
}
