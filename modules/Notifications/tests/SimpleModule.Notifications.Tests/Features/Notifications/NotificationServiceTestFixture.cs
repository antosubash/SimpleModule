using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SimpleModule.Database;
using SimpleModule.Notifications.Contracts;
using SimpleModule.Notifications.Infrastructure;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Notifications.Tests.Features.Notifications;

public abstract class NotificationServiceTestFixture : IDisposable
{
    private readonly NotificationsDbContext _db;
    private bool _disposed;

    protected NotificationsDbContext Db => _db;
    protected NotificationService Sut { get; }
    protected UserId CurrentUserId { get; } = UserId.From("user-1");

    protected NotificationServiceTestFixture()
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
        Sut = new NotificationService(_db);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _db.Dispose();
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected async Task<Notification> SeedAsync(
        UserId? userId = null,
        DateTimeOffset? readAt = null
    )
    {
        var n = new Notification
        {
            Id = NotificationId.From(Guid.CreateVersion7()),
            UserId = userId ?? CurrentUserId,
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
}
