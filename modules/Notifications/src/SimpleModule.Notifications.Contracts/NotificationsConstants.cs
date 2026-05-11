namespace SimpleModule.Notifications.Contracts;

public static class NotificationsConstants
{
    public const string ModuleName = "Notifications";
    public const string RoutePrefix = "/api/notifications";
    public const string ViewPrefix = "/notifications";

    public static class Channels
    {
        public const string Mail = "mail";
        public const string Database = "database";
        public const string Sms = "sms";
    }

    public static class Routes
    {
        // API endpoints
        public const string List = "/";
        public const string UnreadCount = "/unread-count";
        public const string MarkRead = "/{id}/read";
        public const string MarkAllRead = "/read-all";
        public const string Send = "/send";

        // View endpoints
        public const string Inbox = "/";
    }

    public static class SettingsKeys
    {
        public const string MailChannelEnabled = "notifications.channel.mail.enabled";
        public const string DatabaseChannelEnabled = "notifications.channel.database.enabled";
        public const string SmsChannelEnabled = "notifications.channel.sms.enabled";
        public const string DefaultPageSize = "notifications.defaultPageSize";
    }
}
