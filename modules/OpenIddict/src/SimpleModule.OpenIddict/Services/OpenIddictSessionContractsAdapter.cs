using SimpleModule.Core;
using SimpleModule.Identity.Contracts;
using SimpleModule.OpenIddict.Contracts;

namespace SimpleModule.OpenIddict.Services;

#pragma warning disable CA1812
[ManualContractRegistration]
internal sealed class OpenIddictSessionContractsAdapter(ISessionContracts inner)
    : IOpenIddictSessionContracts
{
    public Task<IReadOnlyList<SessionDto>> GetActiveSessionsForUserAsync(
        string userId,
        CancellationToken cancellationToken = default
    ) => inner.GetActiveSessionsForUserAsync(userId, cancellationToken);

    public Task<IReadOnlyList<SessionDto>> GetActiveSessionsForUserAsync(
        string userId,
        string? currentTokenId,
        CancellationToken cancellationToken = default
    ) => inner.GetActiveSessionsForUserAsync(userId, currentTokenId, cancellationToken);

    public Task<RevokeSessionResult> TryRevokeSessionForUserAsync(
        string tokenId,
        string userId,
        string? currentTokenId,
        CancellationToken cancellationToken = default
    ) => inner.TryRevokeSessionForUserAsync(tokenId, userId, currentTokenId, cancellationToken);

    public Task RevokeSessionAsync(string tokenId, CancellationToken cancellationToken = default) =>
        inner.RevokeSessionAsync(tokenId, cancellationToken);

    public Task RevokeAllSessionsForUserAsync(
        string userId,
        CancellationToken cancellationToken = default
    ) => inner.RevokeAllSessionsForUserAsync(userId, cancellationToken);

    public Task RevokeOtherSessionsForUserAsync(
        string userId,
        string? currentTokenId,
        CancellationToken cancellationToken = default
    ) => inner.RevokeOtherSessionsForUserAsync(userId, currentTokenId, cancellationToken);
}
