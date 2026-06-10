using System.Net;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core.Authorization;
using SimpleModule.Notifications.Contracts;
using SimpleModule.Tests.Shared.Fixtures;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Notifications.Tests.Integration;

/// <summary>
/// End-to-end coverage of the policy-based authorization flow: the MarkRead endpoint
/// loads the notification, dispatches to <see cref="NotificationPolicy"/> via
/// IAuthorizer, and ownership denials surface as 404 (DenyAsNotFound) so callers
/// cannot probe other users' notification IDs.
/// </summary>
[Collection(TestCollections.Integration)]
public sealed class MarkReadEndpointTests(SimpleModuleWebApplicationFactory factory)
{
    private async Task<Notification> SeedAsync(string ownerId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        var notification = new Notification
        {
            Id = NotificationId.From(Guid.CreateVersion7()),
            UserId = UserId.From(ownerId),
            Type = "test.event",
            Channel = NotificationsConstants.Channels.Database,
            Title = "Title",
            DataJson = "{}",
        };
        db.Notifications.Add(notification);
        await db.SaveChangesAsync();
        return notification;
    }

    private async Task<DateTimeOffset?> GetReadAtAsync(NotificationId id)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        var notification = await db.Notifications.AsNoTracking().FirstAsync(n => n.Id == id);
        return notification.ReadAt;
    }

    private HttpClient CreateClientFor(string userId, params string[] roles)
    {
        var claims = new List<Claim> { new("sub", userId) };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        return factory.CreateAuthenticatedClient(
            [NotificationsPermissions.ViewOwn],
            [.. claims]
        );
    }

    [Fact]
    public async Task MarkRead_AsOwner_Returns204AndMarksRead()
    {
        var notification = await SeedAsync("owner-1");
        var client = CreateClientFor("owner-1");

        var response = await client.PostAsync(
            $"/api/notifications/{notification.Id.Value}/read",
            null
        );

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await GetReadAtAsync(notification.Id)).Should().NotBeNull();
    }

    [Fact]
    public async Task MarkRead_AsNonOwner_Returns404NotForbidden()
    {
        var notification = await SeedAsync("owner-1");
        var client = CreateClientFor("intruder-2");

        var response = await client.PostAsync(
            $"/api/notifications/{notification.Id.Value}/read",
            null
        );

        // NotificationPolicy denies ownership violations with DenyAsNotFound so
        // callers cannot probe other users' notification IDs.
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await GetReadAtAsync(notification.Id)).Should().BeNull();
    }

    [Fact]
    public async Task MarkRead_AsAdminForOthersNotification_Returns404AndDoesNotMutate()
    {
        // Admins are not exempt from the ownership rule — marking read mutates the
        // recipient's inbox state.
        var notification = await SeedAsync("owner-1");
        var client = CreateClientFor("admin-1", WellKnownRoles.Admin);

        var response = await client.PostAsync(
            $"/api/notifications/{notification.Id.Value}/read",
            null
        );

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await GetReadAtAsync(notification.Id)).Should().BeNull();
    }

    [Fact]
    public async Task MarkRead_MissingNotification_Returns404()
    {
        var client = CreateClientFor("owner-1");

        var response = await client.PostAsync(
            $"/api/notifications/{Guid.CreateVersion7()}/read",
            null
        );

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task MarkRead_Unauthenticated_Returns401()
    {
        var notification = await SeedAsync("owner-1");
        var client = factory.CreateClient();

        var response = await client.PostAsync(
            $"/api/notifications/{notification.Id.Value}/read",
            null
        );

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
