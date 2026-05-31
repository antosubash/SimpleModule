namespace SimpleModule.Identity.Contracts;

public interface ISessionContracts
{
    Task<IReadOnlyList<SessionDto>> GetActiveSessionsForUserAsync(
        string userId,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<SessionDto>> GetActiveSessionsForUserAsync(
        string userId,
        string? currentTokenId,
        CancellationToken cancellationToken = default
    );

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

    Task RevokeOtherSessionsForUserAsync(
        string userId,
        string? currentTokenId,
        CancellationToken cancellationToken = default
    );
}
