namespace SimpleModule.OpenIddict.Contracts;

public enum RevokeSessionResult
{
    /// <summary>The session existed, was owned by the caller, and has been revoked.</summary>
    Revoked,

    /// <summary>The token id was unknown or belonged to a different user. The endpoint
    /// surfaces this as 404 so the response shape doesn't leak whether a token id
    /// exists for someone else.</summary>
    NotFound,

    /// <summary>The token is part of the caller's own session (shares an authorization
    /// with the request's token). Refused to prevent self-lockout.</summary>
    BlockedCurrent,
}

public interface IOpenIddictSessionContracts
{
    /// <summary>
    /// Returns one row per valid token. Used by the admin tab where each token
    /// (access / refresh / rotation) is shown individually.
    /// </summary>
    Task<IReadOnlyList<UserSessionDto>> GetActiveSessionsForUserAsync(
        string userId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Returns one row per authorization (i.e. per login). Tokens sharing an
    /// AuthorizationId collapse to a single "session" entry so the user can't
    /// revoke their refresh token while leaving their access token live, or
    /// vice versa. The DTO's <c>TokenId</c> is the anchor token id used for
    /// subsequent revoke calls; <c>IsCurrent</c> is set when the group contains
    /// <paramref name="currentTokenId"/>.
    /// </summary>
    Task<IReadOnlyList<UserSessionDto>> GetActiveSessionsForUserAsync(
        string userId,
        string? currentTokenId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Revokes the authorization containing <paramref name="tokenId"/>, but only
    /// if it belongs to <paramref name="userId"/> and does not share an
    /// authorization with <paramref name="currentTokenId"/>. Returns a result the
    /// endpoint maps to 200 / 400 / 404. Single-load ownership check defends
    /// against cross-user token-id guessing without a separate query.
    /// </summary>
    Task<RevokeSessionResult> TryRevokeSessionForUserAsync(
        string tokenId,
        string userId,
        string? currentTokenId,
        CancellationToken cancellationToken = default
    );

    Task RevokeSessionAsync(string tokenId, CancellationToken cancellationToken = default);

    Task RevokeAllSessionsForUserAsync(
        string userId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Revokes every valid token for the user except those sharing an authorization
    /// with <paramref name="currentTokenId"/>. When <paramref name="currentTokenId"/>
    /// is null, revokes everything (equivalent to <see cref="RevokeAllSessionsForUserAsync"/>).
    /// </summary>
    Task RevokeOtherSessionsForUserAsync(
        string userId,
        string? currentTokenId,
        CancellationToken cancellationToken = default
    );
}
