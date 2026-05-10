using OpenIddict.Abstractions;
using SimpleModule.OpenIddict.Contracts;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace SimpleModule.OpenIddict.Services;

public sealed class OpenIddictSessionService(
    IOpenIddictTokenManager tokenManager,
    IOpenIddictApplicationManager appManager
) : IOpenIddictSessionContracts
{
    public async Task<IReadOnlyList<UserSessionDto>> GetActiveSessionsForUserAsync(
        string userId,
        CancellationToken cancellationToken = default
    )
    {
        var sessions = new List<UserSessionDto>();
        var appNameCache = new Dictionary<string, string?>();

        await foreach (var token in tokenManager.FindBySubjectAsync(userId, cancellationToken))
        {
            var dto = await BuildDtoAsync(token, appNameCache, cancellationToken);
            if (dto is not null)
                sessions.Add(dto);
        }

        return sessions;
    }

    public async Task<IReadOnlyList<UserSessionDto>> GetActiveSessionsForUserAsync(
        string userId,
        string? currentTokenId,
        CancellationToken cancellationToken = default
    )
    {
        // Resolve the caller's authorization id once so we can flag the matching
        // group as current, even if the rendered anchor is a sibling token.
        var currentAuthorizationId = await GetAuthorizationIdForTokenAsync(
            currentTokenId,
            cancellationToken
        );
        var appNameCache = new Dictionary<string, string?>();

        // Collect valid tokens grouped by authorization. Tokens with no
        // authorization id (rare — non-code grants) stand alone keyed by their
        // own id so they still get a row.
        var groups = new Dictionary<string, List<TokenRow>>(StringComparer.Ordinal);

        await foreach (var token in tokenManager.FindBySubjectAsync(userId, cancellationToken))
        {
            var row = await ReadTokenAsync(token, cancellationToken);
            if (row is null)
                continue;

            var key = row.Value.AuthorizationId ?? $"token:{row.Value.TokenId}";
            if (!groups.TryGetValue(key, out var bucket))
            {
                bucket = new List<TokenRow>();
                groups[key] = bucket;
            }
            bucket.Add(row.Value);
        }

        var sessions = new List<UserSessionDto>(groups.Count);
        foreach (var bucket in groups.Values)
        {
            // Prefer a refresh token as the anchor so the row reflects the longer-
            // lived part of the session; fall back to the newest access token.
            TokenRow? refreshAnchor = null;
            foreach (var row in bucket)
            {
                if (row.Type == TokenTypeHints.RefreshToken)
                {
                    refreshAnchor = row;
                    break;
                }
            }
            var anchor =
                refreshAnchor
                ?? bucket.OrderByDescending(t => t.CreationDate ?? DateTimeOffset.MinValue).First();

            var appName = await ResolveAppNameAsync(
                anchor.ApplicationId,
                appNameCache,
                cancellationToken
            );

            var isCurrent =
                (
                    currentAuthorizationId is not null
                    && string.Equals(
                        anchor.AuthorizationId,
                        currentAuthorizationId,
                        StringComparison.Ordinal
                    )
                )
                || (
                    !string.IsNullOrEmpty(currentTokenId)
                    && bucket.Any(t =>
                        string.Equals(t.TokenId, currentTokenId, StringComparison.Ordinal)
                    )
                );

            sessions.Add(
                new UserSessionDto
                {
                    TokenId = anchor.TokenId,
                    Type = anchor.Type,
                    ApplicationName = appName,
                    CreationDate = bucket.Min(t => t.CreationDate),
                    ExpirationDate = bucket.Max(t => t.ExpirationDate),
                    IsCurrent = isCurrent,
                }
            );
        }

        return sessions;
    }

    public async Task<RevokeSessionResult> TryRevokeSessionForUserAsync(
        string tokenId,
        string userId,
        string? currentTokenId,
        CancellationToken cancellationToken = default
    )
    {
        var target = await tokenManager.FindByIdAsync(tokenId, cancellationToken);
        if (target is null)
            return RevokeSessionResult.NotFound;

        var subject = await tokenManager.GetSubjectAsync(target, cancellationToken);
        if (!string.Equals(subject, userId, StringComparison.Ordinal))
            return RevokeSessionResult.NotFound;

        var targetAuthorizationId = await tokenManager.GetAuthorizationIdAsync(
            target,
            cancellationToken
        );
        var currentAuthorizationId = await GetAuthorizationIdForTokenAsync(
            currentTokenId,
            cancellationToken
        );

        // Self-revoke guard: same authorization, or same token id when no
        // authorization is recorded.
        if (
            targetAuthorizationId is not null
            && currentAuthorizationId is not null
            && string.Equals(
                targetAuthorizationId,
                currentAuthorizationId,
                StringComparison.Ordinal
            )
        )
        {
            return RevokeSessionResult.BlockedCurrent;
        }
        if (
            targetAuthorizationId is null
            && !string.IsNullOrEmpty(currentTokenId)
            && string.Equals(tokenId, currentTokenId, StringComparison.Ordinal)
        )
        {
            return RevokeSessionResult.BlockedCurrent;
        }

        if (targetAuthorizationId is null)
        {
            // No authorization — just revoke this token.
            await tokenManager.TryRevokeAsync(target, cancellationToken);
            return RevokeSessionResult.Revoked;
        }

        // Revoke every token in the same authorization for this user. Materialize
        // first so we don't mutate the store mid-iteration.
        var siblings = new List<object>();
        await foreach (var token in tokenManager.FindBySubjectAsync(userId, cancellationToken))
        {
            var authId = await tokenManager.GetAuthorizationIdAsync(token, cancellationToken);
            if (string.Equals(authId, targetAuthorizationId, StringComparison.Ordinal))
            {
                siblings.Add(token);
            }
        }

        foreach (var token in siblings)
        {
            await tokenManager.TryRevokeAsync(token, cancellationToken);
        }

        return RevokeSessionResult.Revoked;
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
        var currentAuthorizationId = await GetAuthorizationIdForTokenAsync(
            currentTokenId,
            cancellationToken
        );

        var tokensToRevoke = new List<object>();

        await foreach (var token in tokenManager.FindBySubjectAsync(userId, cancellationToken))
        {
            var status = await tokenManager.GetStatusAsync(token, cancellationToken);
            if (status != Statuses.Valid)
                continue;

            // Same-authorization check first; falls through to token-id check for
            // tokens that have no recorded authorization.
            var authId = await tokenManager.GetAuthorizationIdAsync(token, cancellationToken);
            if (
                currentAuthorizationId is not null
                && authId is not null
                && string.Equals(authId, currentAuthorizationId, StringComparison.Ordinal)
            )
            {
                continue;
            }
            if (authId is null && !string.IsNullOrEmpty(currentTokenId))
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

    private async Task<UserSessionDto?> BuildDtoAsync(
        object token,
        Dictionary<string, string?> appNameCache,
        CancellationToken cancellationToken
    )
    {
        var row = await ReadTokenAsync(token, cancellationToken);
        if (row is null)
            return null;

        var appName = await ResolveAppNameAsync(row.Value.ApplicationId, appNameCache, cancellationToken);

        return new UserSessionDto
        {
            TokenId = row.Value.TokenId,
            Type = row.Value.Type,
            ApplicationName = appName,
            CreationDate = row.Value.CreationDate,
            ExpirationDate = row.Value.ExpirationDate,
            IsCurrent = false,
        };
    }

    private async Task<TokenRow?> ReadTokenAsync(object token, CancellationToken cancellationToken)
    {
        var type = await tokenManager.GetTypeAsync(token, cancellationToken);
        if (type is not (TokenTypeHints.AccessToken or TokenTypeHints.RefreshToken))
            return null;

        var status = await tokenManager.GetStatusAsync(token, cancellationToken);
        if (status != Statuses.Valid)
            return null;

        var expiration = await tokenManager.GetExpirationDateAsync(token, cancellationToken);
        if (expiration.HasValue && expiration.Value < DateTimeOffset.UtcNow)
            return null;

        var tokenId = await tokenManager.GetIdAsync(token, cancellationToken) ?? string.Empty;
        var creation = await tokenManager.GetCreationDateAsync(token, cancellationToken);
        var appId = await tokenManager.GetApplicationIdAsync(token, cancellationToken);
        var authorizationId = await tokenManager.GetAuthorizationIdAsync(token, cancellationToken);

        return new TokenRow(
            tokenId,
            type ?? string.Empty,
            appId,
            authorizationId,
            creation,
            expiration
        );
    }

    private async Task<string?> ResolveAppNameAsync(
        string? appId,
        Dictionary<string, string?> cache,
        CancellationToken cancellationToken
    )
    {
        if (appId is null)
            return null;

        if (cache.TryGetValue(appId, out var cached))
            return cached;

        var app = await appManager.FindByIdAsync(appId, cancellationToken);
        var name = app is null ? null : await appManager.GetDisplayNameAsync(app, cancellationToken);
        cache[appId] = name;
        return name;
    }

    private async Task<string?> GetAuthorizationIdForTokenAsync(
        string? tokenId,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrEmpty(tokenId))
            return null;

        var token = await tokenManager.FindByIdAsync(tokenId, cancellationToken);
        if (token is null)
            return null;

        return await tokenManager.GetAuthorizationIdAsync(token, cancellationToken);
    }

    private readonly record struct TokenRow(
        string TokenId,
        string Type,
        string? ApplicationId,
        string? AuthorizationId,
        DateTimeOffset? CreationDate,
        DateTimeOffset? ExpirationDate
    );
}
