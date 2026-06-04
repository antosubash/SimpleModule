using FluentAssertions;
using SimpleModule.Notifications.Contracts.Features.Notifications.List;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Notifications.Tests.Features.Notifications.List;

public sealed class ListAsyncTests : NotificationServiceTestFixture
{
    [Fact]
    public async Task ListAsync_ReturnsOnlyOwnNotifications()
    {
        await SeedAsync();
        await SeedAsync();
        await SeedAsync(userId: UserId.From("other-user"));

        var result = await Sut.ListAsync(CurrentUserId, new QueryNotificationsRequest());

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task ListAsync_UnreadOnly_FiltersReadNotifications()
    {
        await SeedAsync();
        await SeedAsync(readAt: DateTimeOffset.UtcNow);

        var result = await Sut.ListAsync(
            CurrentUserId,
            new QueryNotificationsRequest { UnreadOnly = true }
        );

        result.TotalCount.Should().Be(1);
    }
}
