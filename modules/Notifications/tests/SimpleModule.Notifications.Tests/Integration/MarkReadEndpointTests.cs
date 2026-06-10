using System.Net;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Notifications.Contracts;
using SimpleModule.Tests.Shared.Fixtures;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Notifications.Tests.Integration;

/// <summary>
/// End-to-end coverage of the policy-based authorization flow: the MarkRead endpoint
/// loads the notification, dispatches to <see cref="NotificationPolicy"/> via
/// IAuthorizer, and denial for non-owners surfaces as 404 (configured through
/// PolicyAuthorizationOptions in the module).
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

    private HttpClient CreateClientFor(string userId) =>
        factory.CreateAuthenticatedClient(
            [NotificationsPermissions.ViewOwn],
            new Claim("sub", userId)
        );

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

        using var scope = factory.Services.CreateScope();
        var contracts = scope.ServiceProvider.GetRequiredService<INotificationsContracts>();
        var refreshed = await contracts.FindAsync(notification.Id);
        refreshed!.ReadAt.Should().NotBeNull();
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

        // NotificationsModule maps denied markRead to 404 via PolicyAuthorizationOptions
        // so callers cannot probe other users' notification IDs.
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        using var scope = factory.Services.CreateScope();
        var contracts = scope.ServiceProvider.GetRequiredService<INotificationsContracts>();
        var refreshed = await contracts.FindAsync(notification.Id);
        refreshed!.ReadAt.Should().BeNull();
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
