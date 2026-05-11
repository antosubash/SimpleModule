using System.Text.Json;
using Microsoft.Extensions.Logging;
using SimpleModule.Notifications.Contracts;
using SimpleModule.Notifications.Contracts.Events;
using Wolverine.EntityFrameworkCore;

namespace SimpleModule.Notifications.Channels;

public partial class DatabaseChannel(
    NotificationsDbContext db,
    IDbContextOutbox<NotificationsDbContext> outbox,
    ILogger<DatabaseChannel> logger
) : INotificationChannel
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public string Name => NotificationsConstants.Channels.Database;

    public async Task SendAsync(
        NotificationRecipient recipient,
        INotification notification,
        CancellationToken cancellationToken = default
    )
    {
        var payload = notification.ToDatabase(recipient);
        if (payload is null)
        {
            return;
        }

        var entity = new Notification
        {
            Id = NotificationId.From(Guid.CreateVersion7()),
            UserId = recipient.UserId,
            Type = notification.NotificationType,
            Channel = Name,
            Title = payload.Title,
            Body = payload.Body,
            DataJson = payload.Data is null
                ? "{}"
                : JsonSerializer.Serialize(payload.Data, JsonOptions),
        };

        db.Notifications.Add(entity);

        await outbox.PublishAsync(
            new NotificationSentEvent(recipient.UserId, notification.NotificationType, Name)
        );
        await outbox.SaveChangesAndFlushMessagesAsync(cancellationToken);

        LogPersisted(logger, entity.Id, recipient.UserId.Value, notification.NotificationType);
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Persisted notification {NotificationId} for user {UserId} type {Type}"
    )]
    private static partial void LogPersisted(
        ILogger logger,
        NotificationId notificationId,
        string userId,
        string type
    );
}
