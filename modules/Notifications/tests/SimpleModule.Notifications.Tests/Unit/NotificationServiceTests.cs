using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SimpleModule.Database;
using SimpleModule.Notifications.Contracts;
using SimpleModule.Notifications.Services;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Notifications.Tests.Unit;

public sealed class NotificationServiceTests : IDisposable
{
    private readonly NotificationsDbContext _db;
    private readonly NotificationService _sut;
    private readonly UserId _userId = UserId.From("user-1");

    public NotificationServiceTests()
    {
        var options = new DbContextOptionsBuilder<NotificationsDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        var dbOptions = Options.Create(
            new DatabaseOptions
            {
                ModuleConnections = new Dictionary<string, string>
                {
                    ["Notifications"] = "Data Source=:memory:",
                },
            }
        );
        _db = new NotificationsDbContext(options, dbOptions);
        _db.Database.OpenConnection();
        _db.Database.EnsureCreated();
        _sut = new NotificationService(_db);
    }

    public void Dispose() => _db.Dispose();

    private async Task<Notification> SeedAsync(UserId? userId = null, DateTimeOffset? readAt = null)
    {
        var n = new Notification
        {
            Id = NotificationId.From(Guid.CreateVersion7()),
            UserId = userId ?? _userId,
            Type = "test.event",
            Channel = NotificationsConstants.Channels.Database,
            Title = "Title",
            Body = "Body",
            DataJson = "{}",
            ReadAt = readAt,
        };
        _db.Notifications.Add(n);
        await _db.SaveChangesAsync();
        return n;
    }

    [Fact]
    public async Task ListAsync_ReturnsOnlyOwnNotifications()
    {
        await SeedAsync();
        await SeedAsync();
        await SeedAsync(userId: UserId.From("other-user"));

        var result = await _sut.ListAsync(_userId, new QueryNotificationsRequest());

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task ListAsync_UnreadOnly_FiltersReadNotifications()
    {
        await SeedAsync();
        await SeedAsync(readAt: DateTimeOffset.UtcNow);

        var result = await _sut.ListAsync(
            _userId,
            new QueryNotificationsRequest { UnreadOnly = true }
        );

        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetUnreadCountAsync_ReturnsUnreadOnly()
    {
        await SeedAsync();
        await SeedAsync();
        await SeedAsync(readAt: DateTimeOffset.UtcNow);

        var count = await _sut.GetUnreadCountAsync(_userId);

        count.Should().Be(2);
    }

    [Fact]
    public async Task MarkReadAsync_SetsReadAt()
    {
        var n = await SeedAsync();

        var result = await _sut.MarkReadAsync(n.Id, _userId);

        result.Should().BeTrue();
        var refreshed = await _db.Notifications.AsNoTracking().FirstAsync(x => x.Id == n.Id);
        refreshed.ReadAt.Should().NotBeNull();
    }

    [Fact]
    public async Task MarkReadAsync_WithDifferentUser_ReturnsFalse()
    {
        var n = await SeedAsync();

        var result = await _sut.MarkReadAsync(n.Id, UserId.From("not-the-owner"));

        result.Should().BeFalse();
    }

    [Fact]
    public async Task MarkAllReadAsync_MarksAllUnreadForUser()
    {
        await SeedAsync();
        await SeedAsync();
        await SeedAsync(readAt: DateTimeOffset.UtcNow);
        await SeedAsync(userId: UserId.From("other"));

        var marked = await _sut.MarkAllReadAsync(_userId);

        marked.Should().Be(2);
        var remainingUnread = await _sut.GetUnreadCountAsync(_userId);
        remainingUnread.Should().Be(0);
        var otherUserUnread = await _sut.GetUnreadCountAsync(UserId.From("other"));
        otherUserUnread.Should().Be(1);
    }
}
