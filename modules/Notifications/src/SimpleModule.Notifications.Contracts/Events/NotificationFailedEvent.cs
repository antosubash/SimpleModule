using SimpleModule.Core.Events;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Notifications.Contracts.Events;

public sealed record NotificationFailedEvent(
    UserId UserId,
    string NotificationType,
    string Channel,
    string Error
) : DomainEvent;
