using Microsoft.Extensions.Logging;
using SimpleModule.BackgroundJobs.Contracts;
using SimpleModule.Notifications.Channels;
using SimpleModule.Notifications.Contracts;
using SimpleModule.Notifications.Contracts.Events;
using SimpleModule.Notifications.Jobs;
using Wolverine;

namespace SimpleModule.Notifications.Services;

public class Notifier(
    INotificationChannelRegistry channels,
    IBackgroundJobs backgroundJobs,
    IMessageBus bus,
    ILogger<Notifier> logger
) : INotifier
{
    public async Task SendAsync<T>(
        NotificationRecipient recipient,
        T notification,
        CancellationToken cancellationToken = default
    )
        where T : INotification
    {
        // One job per channel so a slow/failing channel cannot block siblings and so retries are per-channel.
        var channelNames = notification.Via(recipient);
        var notificationJson = System.Text.Json.JsonSerializer.Serialize(
            notification,
            notification.GetType()
        );
        var typeName = typeof(T).AssemblyQualifiedName!;

        foreach (var channelName in channelNames)
        {
            await backgroundJobs.EnqueueAsync<DispatchNotificationJob>(
                new DispatchNotificationJobData(
                    recipient.UserId.Value,
                    recipient.Email,
                    recipient.PhoneNumber,
                    channelName,
                    typeName,
                    notificationJson
                ),
                cancellationToken
            );
        }
    }

    public async Task SendNowAsync<T>(
        NotificationRecipient recipient,
        T notification,
        CancellationToken cancellationToken = default
    )
        where T : INotification
    {
        var channelNames = notification.Via(recipient);

        foreach (var channelName in channelNames)
        {
            var channel = channels.Find(channelName);
            if (channel is null)
            {
                NotificationsLog.UnknownChannel(logger, channelName, notification.NotificationType);
                continue;
            }

            try
            {
                await channel.SendAsync(recipient, notification, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                NotificationsLog.ChannelFailure(
                    logger,
                    channelName,
                    notification.NotificationType,
                    recipient.UserId.Value,
                    ex
                );
                await bus.PublishAsync(
                    new NotificationFailedEvent(
                        recipient.UserId,
                        notification.NotificationType,
                        channelName,
                        ex.Message
                    )
                );
            }
        }
    }
}
