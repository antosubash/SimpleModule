using SimpleModule.Core.Authorization;

namespace SimpleModule.Notifications;

public sealed class NotificationsPermissions : IModulePermissions
{
    public const string ViewOwn = "Notifications.ViewOwn";
    public const string SendToOthers = "Notifications.SendToOthers";
}
