using SimpleModule.Core.Events;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Notifications.Contracts.Events;

public sealed record NotificationSentEvent(
    UserId UserId,
    string NotificationType,
    string Channel
) : DomainEvent;
