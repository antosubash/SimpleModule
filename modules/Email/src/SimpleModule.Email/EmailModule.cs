using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.BackgroundJobs.Contracts;
using SimpleModule.Core;
using SimpleModule.Core.Menu;
using SimpleModule.Core.Settings;
using SimpleModule.Database;
using SimpleModule.Email.Contracts;
using SimpleModule.Email.Jobs;
using SimpleModule.Email.Providers;
using SimpleModule.Email.Services;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Email;

[Module(
    EmailConstants.ModuleName,
    RoutePrefix = EmailConstants.RoutePrefix,
    ViewPrefix = EmailConstants.ViewPrefix
)]
public class EmailModule : IModule, IModuleServices, IModuleMenu
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<EmailDbContext>(configuration, EmailConstants.ModuleName);
        var emailSection = configuration.GetSection("Email");
        services.Configure<EmailModuleOptions>(emailSection);

        services.AddScoped<IEmailContracts, EmailService>();

        var emailOptions = emailSection.Get<EmailModuleOptions>() ?? new EmailModuleOptions();

        if (string.Equals(emailOptions.Provider, "SMTP", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IEmailProvider, SmtpEmailProvider>();
        }
        else
        {
            services.AddScoped<IEmailProvider, LogEmailProvider>();
        }

        // Replace Identity's ConsoleEmailSender with a real one
        services.AddScoped<IEmailSender<ApplicationUser>, IdentityEmailSender>();

        services.AddModuleJob<SendEmailJob>();
        services.AddModuleJob<RetryFailedEmailsJob>();
        services.AddHostedService<EmailJobRegistrationHostedService>();
    }

    public void ConfigureSettings(ISettingsBuilder settings)
    {
        settings
            .Add(
                new SettingDefinition
                {
                    Key = "email.provider",
                    DisplayName = "Email Provider",
                    Description = "The email provider to use (Log, SMTP)",
                    Group = "Email",
                    Scope = SettingScope.System,
                    DefaultValue = "Log",
                    Type = SettingType.Text,
                }
            )
            .Add(
                new SettingDefinition
                {
                    Key = "email.defaultFromAddress",
                    DisplayName = "Default From Address",
                    Description = "Default sender email address",
                    Group = "Email",
                    Scope = SettingScope.System,
                    DefaultValue = "noreply@localhost",
                    Type = SettingType.Text,
                }
            )
            .Add(
                new SettingDefinition
                {
                    Key = "email.maxRetryCount",
                    DisplayName = "Max Retry Count",
                    Description = "Maximum number of retry attempts for failed emails",
                    Group = "Email",
                    Scope = SettingScope.System,
                    DefaultValue = "3",
                    Type = SettingType.Number,
                }
            )
            .Add(
                new SettingDefinition
                {
                    Key = "email.retryIntervalCron",
                    DisplayName = "Retry Interval (Cron)",
                    Description = "Cron expression for retrying failed emails",
                    Group = "Email",
                    Scope = SettingScope.System,
                    DefaultValue = "*/5 * * * *",
                    Type = SettingType.Text,
                }
            );
    }

    public void ConfigureMenu(IMenuBuilder menus)
    {
        menus.Add(
            new MenuItem
            {
                Label = "Email Dashboard",
                Url = "/email/dashboard",
                Icon =
                    """<svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z"/></svg>""",
                Order = 49,
                Section = MenuSection.AdminSidebar,
            }
        );
        menus.Add(
            new MenuItem
            {
                Label = "Email Templates",
                Url = "/email/templates",
                Icon =
                    """<svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z"/></svg>""",
                Order = 50,
                Section = MenuSection.AdminSidebar,
            }
        );
        menus.Add(
            new MenuItem
            {
                Label = "Email History",
                Url = "/email/history",
                Icon =
                    """<svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"/></svg>""",
                Order = 51,
                Section = MenuSection.AdminSidebar,
            }
        );
    }
}
