using FluentAssertions;

namespace SimpleModule.Notifications.Tests.Features.Notifications.UnreadCount;

public sealed class GetUnreadCountAsyncTests : NotificationServiceTestFixture
{
    [Fact]
    public async Task GetUnreadCountAsync_ReturnsUnreadOnly()
    {
        await SeedAsync();
        await SeedAsync();
        await SeedAsync(readAt: DateTimeOffset.UtcNow);

        var count = await Sut.GetUnreadCountAsync(CurrentUserId);

        count.Should().Be(2);
    }
}
