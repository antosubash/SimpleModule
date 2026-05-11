using System.Text.Json;
using Microsoft.Extensions.Logging;
using SimpleModule.BackgroundJobs.Contracts;
using SimpleModule.Notifications.Channels;
using SimpleModule.Notifications.Contracts;
using SimpleModule.Notifications.Contracts.Events;
using SimpleModule.Notifications.Services;
using SimpleModule.Users.Contracts;
using Wolverine;

namespace SimpleModule.Notifications.Jobs;

public sealed record DispatchNotificationJobData(
    string UserId,
    string? Email,
    string? PhoneNumber,
    string ChannelName,
    string NotificationTypeName,
    string NotificationJson
);

public class DispatchNotificationJob(
    INotificationChannelRegistry channels,
    IMessageBus bus,
    ILogger<DispatchNotificationJob> logger
) : IModuleJob
{
    public async Task ExecuteAsync(
        IJobExecutionContext context,
        CancellationToken cancellationToken
    )
    {
        var jobData = context.GetData<DispatchNotificationJobData>();

        var channel = channels.Find(jobData.ChannelName);
        if (channel is null)
        {
            context.Log($"Unknown channel '{jobData.ChannelName}'; dropping notification.");
            return;
        }

        var notificationType = Type.GetType(jobData.NotificationTypeName);
        if (notificationType is null)
        {
            context.Log(
                $"Unable to resolve notification type {jobData.NotificationTypeName}; dropping."
            );
            return;
        }

        if (
            JsonSerializer.Deserialize(jobData.NotificationJson, notificationType)
            is not INotification notification
        )
        {
            context.Log($"Failed to deserialize notification of type {notificationType.Name}.");
            return;
        }

        var recipient = new NotificationRecipient(
            UserId.From(jobData.UserId),
            jobData.Email,
            jobData.PhoneNumber
        );

        try
        {
            await channel.SendAsync(recipient, notification, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            NotificationsLog.ChannelFailure(
                logger,
                jobData.ChannelName,
                notification.NotificationType,
                jobData.UserId,
                ex
            );

            await bus.PublishAsync(
                new NotificationFailedEvent(
                    recipient.UserId,
                    notification.NotificationType,
                    jobData.ChannelName,
                    ex.Message
                )
            );
            throw;
        }

        context.ReportProgress(100);
    }
}
