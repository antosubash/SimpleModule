using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Notifications.Tests.Features.Notifications.MarkRead;

public sealed class MarkReadAsyncTests : NotificationServiceTestFixture
{
    [Fact]
    public async Task MarkReadAsync_SetsReadAt()
    {
        var n = await SeedAsync();

        var result = await Sut.MarkReadAsync(n.Id, CurrentUserId);

        result.Should().BeTrue();
        var refreshed = await Db.Notifications.AsNoTracking().FirstAsync(x => x.Id == n.Id);
        refreshed.ReadAt.Should().NotBeNull();
    }

    [Fact]
    public async Task MarkReadAsync_WithDifferentUser_ReturnsFalse()
    {
        var n = await SeedAsync();

        var result = await Sut.MarkReadAsync(n.Id, UserId.From("not-the-owner"));

        result.Should().BeFalse();
    }
}
