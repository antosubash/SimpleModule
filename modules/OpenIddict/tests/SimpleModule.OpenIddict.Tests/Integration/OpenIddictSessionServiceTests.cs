using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using SimpleModule.Identity.Contracts;
using SimpleModule.OpenIddict.Contracts;
using SimpleModule.Tests.Shared.Fixtures;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace OpenIddict.Tests.Integration;

/// <summary>
/// Behavioural tests for <see cref="IOpenIddictSessionContracts"/>: token-pair
/// grouping by AuthorizationId, IsCurrent across siblings, and the revoke
/// guarantees the user-facing endpoints rely on.
/// </summary>
[Collection(TestCollections.Integration)]
public class OpenIddictSessionServiceTests
{
    private readonly SimpleModuleWebApplicationFactory _factory;

    public OpenIddictSessionServiceTests(SimpleModuleWebApplicationFactory factory)
    {
        _factory = factory;
        // Force database initialization once so OpenIddict managers can resolve.
        using (_factory.CreateAuthenticatedClient()) { }
    }

    // ── Seeding ────────────────────────────────────────────────────────

    private static async Task<string> SeedAuthorizationAsync(
        string userId,
        IServiceProvider services
    )
    {
        var authManager = services.GetRequiredService<IOpenIddictAuthorizationManager>();
        var auth = await authManager.CreateAsync(
            new OpenIddictAuthorizationDescriptor
            {
                Subject = userId,
                Status = Statuses.Valid,
                Type = AuthorizationTypes.Permanent,
            }
        );
        return (await authManager.GetIdAsync(auth))!;
    }

    private static async Task<string> SeedTokenAsync(
        string userId,
        string authorizationId,
        string type,
        IServiceProvider services,
        DateTimeOffset? creationDate = null
    )
    {
        var tokenManager = services.GetRequiredService<IOpenIddictTokenManager>();
        var descriptor = new OpenIddictTokenDescriptor
        {
            Subject = userId,
            AuthorizationId = authorizationId,
            Type = type,
            Status = Statuses.Valid,
            CreationDate = creationDate ?? DateTimeOffset.UtcNow,
            ExpirationDate = DateTimeOffset.UtcNow.AddDays(30),
        };
        var token = await tokenManager.CreateAsync(descriptor);
        return (await tokenManager.GetIdAsync(token))!;
    }

    private static string NewUserId(
        [System.Runtime.CompilerServices.CallerMemberName] string caller = ""
    ) => $"sess-svc-{caller}-{Guid.NewGuid():N}";

    // ── Grouping ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetActiveSessions_GroupsAccessAndRefreshSharingAuthorization_IntoOneRow()
    {
        var userId = NewUserId();
        using var scope = _factory.Services.CreateScope();
        var authId = await SeedAuthorizationAsync(userId, scope.ServiceProvider);
        await SeedTokenAsync(userId, authId, TokenTypeHints.AccessToken, scope.ServiceProvider);
        await SeedTokenAsync(userId, authId, TokenTypeHints.RefreshToken, scope.ServiceProvider);

        var contracts = scope.ServiceProvider.GetRequiredService<IOpenIddictSessionContracts>();
        var sessions = await contracts.GetActiveSessionsForUserAsync(userId, currentTokenId: null);

        sessions.Should().HaveCount(1);
        // Refresh token is preferred as the anchor (longer-lived row).
        sessions[0].Type.Should().Be(TokenTypeHints.RefreshToken);
    }

    [Fact]
    public async Task GetActiveSessions_MultipleAuthorizations_OneRowEach()
    {
        var userId = NewUserId();
        using var scope = _factory.Services.CreateScope();
        var auth1 = await SeedAuthorizationAsync(userId, scope.ServiceProvider);
        var auth2 = await SeedAuthorizationAsync(userId, scope.ServiceProvider);
        await SeedTokenAsync(userId, auth1, TokenTypeHints.RefreshToken, scope.ServiceProvider);
        await SeedTokenAsync(userId, auth2, TokenTypeHints.RefreshToken, scope.ServiceProvider);

        var contracts = scope.ServiceProvider.GetRequiredService<IOpenIddictSessionContracts>();
        var sessions = await contracts.GetActiveSessionsForUserAsync(userId, currentTokenId: null);

        sessions.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetActiveSessions_IsCurrent_SetForBothSiblingsOfTheCallersToken()
    {
        // The principal carries the access token id; the rendered row uses the
        // refresh token id (anchor). The IsCurrent flag must still come back true.
        var userId = NewUserId();
        using var scope = _factory.Services.CreateScope();
        var authId = await SeedAuthorizationAsync(userId, scope.ServiceProvider);
        var accessId = await SeedTokenAsync(
            userId,
            authId,
            TokenTypeHints.AccessToken,
            scope.ServiceProvider
        );
        await SeedTokenAsync(userId, authId, TokenTypeHints.RefreshToken, scope.ServiceProvider);

        var contracts = scope.ServiceProvider.GetRequiredService<IOpenIddictSessionContracts>();
        var sessions = await contracts.GetActiveSessionsForUserAsync(
            userId,
            currentTokenId: accessId
        );

        sessions.Should().HaveCount(1);
        sessions[0].IsCurrent.Should().BeTrue();
    }

    [Fact]
    public async Task GetActiveSessions_FlatOverload_ReturnsOneRowPerToken()
    {
        // The admin-facing overload (no currentTokenId) intentionally keeps a
        // row per token so each can be revoked individually.
        var userId = NewUserId();
        using var scope = _factory.Services.CreateScope();
        var authId = await SeedAuthorizationAsync(userId, scope.ServiceProvider);
        await SeedTokenAsync(userId, authId, TokenTypeHints.AccessToken, scope.ServiceProvider);
        await SeedTokenAsync(userId, authId, TokenTypeHints.RefreshToken, scope.ServiceProvider);

        var contracts = scope.ServiceProvider.GetRequiredService<IOpenIddictSessionContracts>();
        var sessions = await contracts.GetActiveSessionsForUserAsync(userId);

        sessions.Should().HaveCount(2);
    }

    // ── TryRevokeSessionForUserAsync ───────────────────────────────────

    [Fact]
    public async Task TryRevoke_SelfRevoke_ReturnsBlockedCurrent_AndDoesNotRevoke()
    {
        var userId = NewUserId();
        using var scope = _factory.Services.CreateScope();
        var authId = await SeedAuthorizationAsync(userId, scope.ServiceProvider);
        var accessId = await SeedTokenAsync(
            userId,
            authId,
            TokenTypeHints.AccessToken,
            scope.ServiceProvider
        );
        var refreshId = await SeedTokenAsync(
            userId,
            authId,
            TokenTypeHints.RefreshToken,
            scope.ServiceProvider
        );

        var contracts = scope.ServiceProvider.GetRequiredService<IOpenIddictSessionContracts>();

        // Targeting either sibling must refuse when currentTokenId belongs to
        // the same authorization.
        var resultRefresh = await contracts.TryRevokeSessionForUserAsync(
            refreshId,
            userId,
            currentTokenId: accessId
        );
        var resultAccess = await contracts.TryRevokeSessionForUserAsync(
            accessId,
            userId,
            currentTokenId: accessId
        );

        resultRefresh.Should().Be(RevokeSessionResult.BlockedCurrent);
        resultAccess.Should().Be(RevokeSessionResult.BlockedCurrent);

        var tokenManager = scope.ServiceProvider.GetRequiredService<IOpenIddictTokenManager>();
        var refreshToken = await tokenManager.FindByIdAsync(refreshId);
        (await tokenManager.GetStatusAsync(refreshToken!)).Should().Be(Statuses.Valid);
    }

    [Fact]
    public async Task TryRevoke_OwnedTokenInDifferentAuthorization_RevokesAllSiblings()
    {
        var userId = NewUserId();
        using var scope = _factory.Services.CreateScope();
        var currentAuth = await SeedAuthorizationAsync(userId, scope.ServiceProvider);
        var currentAccess = await SeedTokenAsync(
            userId,
            currentAuth,
            TokenTypeHints.AccessToken,
            scope.ServiceProvider
        );

        var otherAuth = await SeedAuthorizationAsync(userId, scope.ServiceProvider);
        var otherAccess = await SeedTokenAsync(
            userId,
            otherAuth,
            TokenTypeHints.AccessToken,
            scope.ServiceProvider
        );
        var otherRefresh = await SeedTokenAsync(
            userId,
            otherAuth,
            TokenTypeHints.RefreshToken,
            scope.ServiceProvider
        );

        var contracts = scope.ServiceProvider.GetRequiredService<IOpenIddictSessionContracts>();
        var result = await contracts.TryRevokeSessionForUserAsync(
            otherRefresh,
            userId,
            currentTokenId: currentAccess
        );

        result.Should().Be(RevokeSessionResult.Revoked);

        var tokenManager = scope.ServiceProvider.GetRequiredService<IOpenIddictTokenManager>();
        // Both siblings in the targeted authorization are revoked …
        (await tokenManager.GetStatusAsync((await tokenManager.FindByIdAsync(otherAccess))!))
            .Should()
            .NotBe(Statuses.Valid);
        (await tokenManager.GetStatusAsync((await tokenManager.FindByIdAsync(otherRefresh))!))
            .Should()
            .NotBe(Statuses.Valid);
        // … and the caller's current session is untouched.
        (await tokenManager.GetStatusAsync((await tokenManager.FindByIdAsync(currentAccess))!))
            .Should()
            .Be(Statuses.Valid);
    }

    [Fact]
    public async Task TryRevoke_TokenOwnedByDifferentUser_ReturnsNotFound()
    {
        var userId = NewUserId();
        var otherUserId = NewUserId() + "-other";
        using var scope = _factory.Services.CreateScope();
        var otherAuth = await SeedAuthorizationAsync(otherUserId, scope.ServiceProvider);
        var otherToken = await SeedTokenAsync(
            otherUserId,
            otherAuth,
            TokenTypeHints.AccessToken,
            scope.ServiceProvider
        );

        var contracts = scope.ServiceProvider.GetRequiredService<IOpenIddictSessionContracts>();
        var result = await contracts.TryRevokeSessionForUserAsync(
            otherToken,
            userId,
            currentTokenId: null
        );

        result.Should().Be(RevokeSessionResult.NotFound);

        var tokenManager = scope.ServiceProvider.GetRequiredService<IOpenIddictTokenManager>();
        (await tokenManager.GetStatusAsync((await tokenManager.FindByIdAsync(otherToken))!))
            .Should()
            .Be(Statuses.Valid);
    }

    [Fact]
    public async Task TryRevoke_UnknownTokenId_ReturnsNotFound()
    {
        var userId = NewUserId();
        using var scope = _factory.Services.CreateScope();
        var contracts = scope.ServiceProvider.GetRequiredService<IOpenIddictSessionContracts>();

        var result = await contracts.TryRevokeSessionForUserAsync(
            tokenId: "does-not-exist",
            userId,
            currentTokenId: null
        );

        result.Should().Be(RevokeSessionResult.NotFound);
    }

    // ── RevokeOtherSessionsForUserAsync ────────────────────────────────

    [Fact]
    public async Task RevokeOthers_PreservesCurrentAuthorization_RevokesEverythingElse()
    {
        var userId = NewUserId();
        using var scope = _factory.Services.CreateScope();
        var currentAuth = await SeedAuthorizationAsync(userId, scope.ServiceProvider);
        var currentAccess = await SeedTokenAsync(
            userId,
            currentAuth,
            TokenTypeHints.AccessToken,
            scope.ServiceProvider
        );
        var currentRefresh = await SeedTokenAsync(
            userId,
            currentAuth,
            TokenTypeHints.RefreshToken,
            scope.ServiceProvider
        );

        var otherAuth = await SeedAuthorizationAsync(userId, scope.ServiceProvider);
        var otherAccess = await SeedTokenAsync(
            userId,
            otherAuth,
            TokenTypeHints.AccessToken,
            scope.ServiceProvider
        );

        var contracts = scope.ServiceProvider.GetRequiredService<IOpenIddictSessionContracts>();
        await contracts.RevokeOtherSessionsForUserAsync(userId, currentTokenId: currentAccess);

        var tokenManager = scope.ServiceProvider.GetRequiredService<IOpenIddictTokenManager>();
        // Current authorization's tokens — including the refresh sibling — survive.
        (await tokenManager.GetStatusAsync((await tokenManager.FindByIdAsync(currentAccess))!))
            .Should()
            .Be(Statuses.Valid);
        (await tokenManager.GetStatusAsync((await tokenManager.FindByIdAsync(currentRefresh))!))
            .Should()
            .Be(Statuses.Valid);
        // The other authorization is gone.
        (await tokenManager.GetStatusAsync((await tokenManager.FindByIdAsync(otherAccess))!))
            .Should()
            .NotBe(Statuses.Valid);
    }

    [Fact]
    public async Task RevokeOthers_NullCurrentToken_RevokesEverythingForUser()
    {
        var userId = NewUserId();
        using var scope = _factory.Services.CreateScope();
        var authId = await SeedAuthorizationAsync(userId, scope.ServiceProvider);
        var tokenId = await SeedTokenAsync(
            userId,
            authId,
            TokenTypeHints.AccessToken,
            scope.ServiceProvider
        );

        var contracts = scope.ServiceProvider.GetRequiredService<IOpenIddictSessionContracts>();
        await contracts.RevokeOtherSessionsForUserAsync(userId, currentTokenId: null);

        var tokenManager = scope.ServiceProvider.GetRequiredService<IOpenIddictTokenManager>();
        (await tokenManager.GetStatusAsync((await tokenManager.FindByIdAsync(tokenId))!))
            .Should()
            .NotBe(Statuses.Valid);
    }
}
