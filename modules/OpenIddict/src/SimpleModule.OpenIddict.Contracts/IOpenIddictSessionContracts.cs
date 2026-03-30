namespace SimpleModule.OpenIddict.Contracts;

public interface IOpenIddictSessionContracts
{
    Task<IReadOnlyList<UserSessionDto>> GetActiveSessionsForUserAsync(
        string userId,
        CancellationToken cancellationToken = default
    );

    Task RevokeSessionAsync(string tokenId, CancellationToken cancellationToken = default);

    Task RevokeAllSessionsForUserAsync(
        string userId,
        CancellationToken cancellationToken = default
    );
}
