using SimpleModule.Notifications.Contracts;

namespace SimpleModule.Notifications.Infrastructure;

public sealed partial class NotificationService(NotificationsDbContext db)
    : INotificationsContracts { }
