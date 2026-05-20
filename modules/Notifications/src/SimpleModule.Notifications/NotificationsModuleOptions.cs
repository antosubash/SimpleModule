using SimpleModule.Core;

namespace SimpleModule.Notifications;

public class NotificationsModuleOptions : IModuleOptions
{
    public bool MailChannelEnabled { get; set; } = true;
    public bool DatabaseChannelEnabled { get; set; } = true;
    public bool SmsChannelEnabled { get; set; }
    public int DefaultPageSize { get; set; } = 20;
}
