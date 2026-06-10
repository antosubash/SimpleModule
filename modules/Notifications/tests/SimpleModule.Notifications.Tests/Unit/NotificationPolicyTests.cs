using System.Security.Claims;
using FluentAssertions;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Authorization.Policies;
using SimpleModule.Notifications.Contracts;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Notifications.Tests.Unit;

public sealed class NotificationPolicyTests
{
    private readonly NotificationPolicy _sut = new();

    private static ClaimsPrincipal CreateUser(string userId, params string[] roles)
    {
        var claims = new List<Claim> { new("sub", userId) };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test", "name", ClaimTypes.Role));
    }

    private static Notification CreateNotification(string ownerId) =>
        new()
        {
            Id = NotificationId.From(Guid.CreateVersion7()),
            UserId = UserId.From(ownerId),
            Type = "test.event",
            Channel = NotificationsConstants.Channels.Database,
        };

    [Theory]
    [InlineData(PolicyActions.View)]
    [InlineData(NotificationPolicy.MarkRead)]
    public async Task Owner_IsAllowed(string action)
    {
        var result = await _sut.AuthorizeAsync(
            CreateUser("user-1"),
            action,
            CreateNotification("user-1")
        );

        result.IsAllowed.Should().BeTrue();
    }

    [Theory]
    [InlineData(PolicyActions.View)]
    [InlineData(NotificationPolicy.MarkRead)]
    public async Task NonOwner_IsDenied(string action)
    {
        var result = await _sut.AuthorizeAsync(
            CreateUser("user-2"),
            action,
            CreateNotification("user-1")
        );

        result.IsAllowed.Should().BeFalse();
        result.Reason.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Admin_IsAllowedForOthersNotifications()
    {
        var result = await _sut.AuthorizeAsync(
            CreateUser("admin-1", WellKnownRoles.Admin),
            NotificationPolicy.MarkRead,
            CreateNotification("user-1")
        );

        result.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task UnknownAction_IsDenied()
    {
        var result = await _sut.AuthorizeAsync(
            CreateUser("user-1"),
            "transmogrify",
            CreateNotification("user-1")
        );

        result.IsAllowed.Should().BeFalse();
    }

    [Fact]
    public async Task UserWithoutIdClaim_IsDenied()
    {
        var anonymous = new ClaimsPrincipal(new ClaimsIdentity());

        var result = await _sut.AuthorizeAsync(
            anonymous,
            PolicyActions.View,
            CreateNotification("user-1")
        );

        result.IsAllowed.Should().BeFalse();
    }
}
