using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.BackgroundJobs.Contracts;
using SimpleModule.Core;
using SimpleModule.Core.Settings;
using SimpleModule.Database;
using SimpleModule.Notifications.Channels;
using SimpleModule.Notifications.Contracts;
using SimpleModule.Notifications.Jobs;
using SimpleModule.Notifications.Services;

namespace SimpleModule.Notifications;

[Module(
    NotificationsConstants.ModuleName,
    RoutePrefix = NotificationsConstants.RoutePrefix,
    ViewPrefix = NotificationsConstants.ViewPrefix
)]
public class NotificationsModule : IModule, IModuleServices
{
    public void ConfigureMiddleware(IApplicationBuilder app)
    {
        // DispatchNotificationJob and the Notifier rely on IBackgroundJobs (whose
        // implementation lives in the BackgroundJobs module, not its Contracts assembly).
        // Fail fast with a directive message if the implementation isn't installed.
        var probe = app.ApplicationServices.GetRequiredService<IServiceProviderIsService>();
        if (!probe.IsService(typeof(IBackgroundJobs)))
        {
            throw new InvalidOperationException(
                "SimpleModule.Notifications requires SimpleModule.BackgroundJobs to be installed. "
                    + "Add a reference to the SimpleModule.BackgroundJobs project so IBackgroundJobs can be resolved."
            );
        }
    }

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<NotificationsDbContext>(
            configuration,
            NotificationsConstants.ModuleName
        );
        services.Configure<NotificationsModuleOptions>(configuration.GetSection("Notifications"));

        services.AddScoped<INotificationsContracts, NotificationService>();
        services.AddScoped<INotifier, Notifier>();

        // Channels — registered as INotificationChannel so the registry can enumerate them.
        services.AddScoped<INotificationChannel, DatabaseChannel>();
        services.AddScoped<INotificationChannel, MailChannel>();
        services.AddScoped<INotificationChannel, LogSmsChannel>();
        services.AddScoped<INotificationChannelRegistry, NotificationChannelRegistry>();

        services.AddModuleJob<DispatchNotificationJob>();
    }

    public void ConfigureSettings(ISettingsBuilder settings)
    {
        settings
            .Add(
                new SettingDefinition
                {
                    Key = NotificationsConstants.SettingsKeys.MailChannelEnabled,
                    DisplayName = "Mail notifications",
                    Description = "Receive notifications by email.",
                    Group = "Notifications",
                    Scope = SettingScope.User,
                    DefaultValue = "true",
                    Type = SettingType.Bool,
                }
            )
            .Add(
                new SettingDefinition
                {
                    Key = NotificationsConstants.SettingsKeys.DatabaseChannelEnabled,
                    DisplayName = "In-app notifications",
                    Description = "Show notifications in the in-app inbox.",
                    Group = "Notifications",
                    Scope = SettingScope.User,
                    DefaultValue = "true",
                    Type = SettingType.Bool,
                }
            )
            .Add(
                new SettingDefinition
                {
                    Key = NotificationsConstants.SettingsKeys.SmsChannelEnabled,
                    DisplayName = "SMS notifications",
                    Description = "Receive notifications by SMS (when a phone number is on file).",
                    Group = "Notifications",
                    Scope = SettingScope.User,
                    DefaultValue = "false",
                    Type = SettingType.Bool,
                }
            )
            .Add(
                new SettingDefinition
                {
                    Key = NotificationsConstants.SettingsKeys.DefaultPageSize,
                    DisplayName = "Inbox page size",
                    Description = "Notifications to load per page in the inbox.",
                    Group = "Notifications",
                    Scope = SettingScope.Application,
                    DefaultValue = "20",
                    Type = SettingType.Number,
                }
            );
    }
}
