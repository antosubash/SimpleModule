using FluentAssertions;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Notifications.Tests.Features.Notifications.MarkAllRead;

public sealed class MarkAllReadAsyncTests : NotificationServiceTestFixture
{
    [Fact]
    public async Task MarkAllReadAsync_MarksAllUnreadForUser()
    {
        await SeedAsync();
        await SeedAsync();
        await SeedAsync(readAt: DateTimeOffset.UtcNow);
        await SeedAsync(userId: UserId.From("other"));

        var marked = await Sut.MarkAllReadAsync(CurrentUserId);

        marked.Should().Be(2);
        var remainingUnread = await Sut.GetUnreadCountAsync(CurrentUserId);
        remainingUnread.Should().Be(0);
        var otherUserUnread = await Sut.GetUnreadCountAsync(UserId.From("other"));
        otherUserUnread.Should().Be(1);
    }
}
