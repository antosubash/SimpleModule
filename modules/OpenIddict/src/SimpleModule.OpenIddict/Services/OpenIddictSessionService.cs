using OpenIddict.Abstractions;
using SimpleModule.OpenIddict.Contracts;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace SimpleModule.OpenIddict.Services;

public sealed class OpenIddictSessionService(
    IOpenIddictTokenManager tokenManager,
    IOpenIddictApplicationManager appManager
) : IOpenIddictSessionContracts
{
    public Task<IReadOnlyList<UserSessionDto>> GetActiveSessionsForUserAsync(
        string userId,
        CancellationToken cancellationToken = default
    ) => GetActiveSessionsForUserAsync(userId, currentTokenId: null, cancellationToken);

    public async Task<IReadOnlyList<UserSessionDto>> GetActiveSessionsForUserAsync(
        string userId,
        string? currentTokenId,
        CancellationToken cancellationToken = default
    )
    {
        var sessions = new List<UserSessionDto>();
        var appNameCache = new Dictionary<string, string?>();

        await foreach (var token in tokenManager.FindBySubjectAsync(userId, cancellationToken))
        {
            var type = await tokenManager.GetTypeAsync(token, cancellationToken);
            if (type is not (TokenTypeHints.AccessToken or TokenTypeHints.RefreshToken))
                continue;

            var status = await tokenManager.GetStatusAsync(token, cancellationToken);
            if (status != Statuses.Valid)
                continue;

            var expiration = await tokenManager.GetExpirationDateAsync(token, cancellationToken);
            if (expiration.HasValue && expiration.Value < DateTimeOffset.UtcNow)
                continue;

            var appId = await tokenManager.GetApplicationIdAsync(token, cancellationToken);
            string? appName = null;
            if (appId is not null)
            {
                if (!appNameCache.TryGetValue(appId, out appName))
                {
                    var app = await appManager.FindByIdAsync(appId, cancellationToken);
                    if (app is not null)
                        appName = await appManager.GetDisplayNameAsync(app, cancellationToken);
                    appNameCache[appId] = appName;
                }
            }

            var tokenId =
                await tokenManager.GetIdAsync(token, cancellationToken) ?? string.Empty;

            sessions.Add(
                new UserSessionDto
                {
                    TokenId = tokenId,
                    Type = type ?? string.Empty,
                    ApplicationName = appName,
                    CreationDate = await tokenManager.GetCreationDateAsync(
                        token,
                        cancellationToken
                    ),
                    ExpirationDate = expiration,
                    IsCurrent =
                        !string.IsNullOrEmpty(currentTokenId)
                        && string.Equals(tokenId, currentTokenId, StringComparison.Ordinal),
                }
            );
        }

        return sessions;
    }

    public async Task<bool> TryRevokeSessionForUserAsync(
        string tokenId,
        string userId,
        CancellationToken cancellationToken = default
    )
    {
        var token = await tokenManager.FindByIdAsync(tokenId, cancellationToken);
        if (token is null)
            return false;

        var subject = await tokenManager.GetSubjectAsync(token, cancellationToken);
        if (!string.Equals(subject, userId, StringComparison.Ordinal))
            return false;

        await tokenManager.TryRevokeAsync(token, cancellationToken);
        return true;
    }

    public async Task RevokeSessionAsync(
        string tokenId,
        CancellationToken cancellationToken = default
    )
    {
        var token = await tokenManager.FindByIdAsync(tokenId, cancellationToken);
        if (token is not null)
        {
            await tokenManager.TryRevokeAsync(token, cancellationToken);
        }
    }

    public Task RevokeAllSessionsForUserAsync(
        string userId,
        CancellationToken cancellationToken = default
    ) => RevokeOtherSessionsForUserAsync(userId, currentTokenId: null, cancellationToken);

    public async Task RevokeOtherSessionsForUserAsync(
        string userId,
        string? currentTokenId,
        CancellationToken cancellationToken = default
    )
    {
        // Materialize valid tokens first; revoking inside the FindBySubjectAsync
        // enumeration could mutate the underlying store mid-iteration.
        var tokensToRevoke = new List<object>();

        await foreach (var token in tokenManager.FindBySubjectAsync(userId, cancellationToken))
        {
            var status = await tokenManager.GetStatusAsync(token, cancellationToken);
            if (status != Statuses.Valid)
                continue;

            if (!string.IsNullOrEmpty(currentTokenId))
            {
                var tokenId = await tokenManager.GetIdAsync(token, cancellationToken);
                if (string.Equals(tokenId, currentTokenId, StringComparison.Ordinal))
                    continue;
            }

            tokensToRevoke.Add(token);
        }

        foreach (var token in tokensToRevoke)
        {
            await tokenManager.TryRevokeAsync(token, cancellationToken);
        }
    }
}
