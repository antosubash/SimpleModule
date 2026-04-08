using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.BackgroundJobs.Contracts;
using SimpleModule.Core;
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
public class EmailModule : IModule, IModuleServices
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

    // Menu items removed — accessible via Admin hub page
}
