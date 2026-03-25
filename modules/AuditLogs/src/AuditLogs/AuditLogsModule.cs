using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.AuditLogs.Contracts;
using SimpleModule.AuditLogs.Interceptors;
using SimpleModule.AuditLogs.Middleware;
using SimpleModule.AuditLogs.Pipeline;
using SimpleModule.AuditLogs.Retention;
using SimpleModule.Core;
using SimpleModule.Core.Events;
using SimpleModule.Core.Menu;
using SimpleModule.Core.Settings;
using SimpleModule.Database;
using SimpleModule.Settings.Contracts;

namespace SimpleModule.AuditLogs;

[Module(
    AuditLogsConstants.ModuleName,
    RoutePrefix = AuditLogsConstants.RoutePrefix,
    ViewPrefix = "/audit-logs"
)]
public class AuditLogsModule : IModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<AuditLogsDbContext>(
            configuration,
            AuditLogsConstants.ModuleName
        );
        services.AddScoped<IAuditContext, AuditContext>();
        services.AddScoped<ISaveChangesInterceptor, AuditSaveChangesInterceptor>();
        services.AddSingleton<AuditChannel>();
        services.AddHostedService<AuditWriterService>();
        services.AddHostedService<AuditRetentionService>();

        // Decorate IEventBus with auditing
        services.AddScoped<IEventBus>(sp =>
        {
            var innerBus = ActivatorUtilities.CreateInstance<SimpleModule.Core.Events.EventBus>(sp);
            var auditCtx = sp.GetRequiredService<IAuditContext>();
            var auditChan = sp.GetRequiredService<AuditChannel>();
            var settingsContracts = sp.GetService<ISettingsContracts>();
            return new AuditingEventBus(innerBus, auditCtx, auditChan, settingsContracts);
        });
    }

    public void ConfigureMiddleware(IApplicationBuilder app)
    {
        app.UseMiddleware<AuditMiddleware>();
    }

    public void ConfigureMenu(IMenuBuilder menus)
    {
        menus.Add(
            new MenuItem
            {
                Label = "Audit Logs",
                Url = "/audit-logs/browse",
                Icon =
                    """<svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" d="M9 12h3.75M9 15h3.75M9 18h3.75m3 .75H18a2.25 2.25 0 002.25-2.25V6.108c0-1.135-.845-2.098-1.976-2.192a48.424 48.424 0 00-1.123-.08m-5.801 0c-.065.21-.1.433-.1.664 0 .414.336.75.75.75h4.5a.75.75 0 00.75-.75 2.25 2.25 0 00-.1-.664m-5.8 0A2.251 2.251 0 0113.5 2.25H15c1.012 0 1.867.668 2.15 1.586m-5.8 0c-.376.023-.75.05-1.124.08C9.095 4.01 8.25 4.973 8.25 6.108V8.25m0 0H4.875c-.621 0-1.125.504-1.125 1.125v11.25c0 .621.504 1.125 1.125 1.125h9.75c.621 0 1.125-.504 1.125-1.125V9.375c0-.621-.504-1.125-1.125-1.125H8.25zM6.75 12h.008v.008H6.75V12zm0 3h.008v.008H6.75V15zm0 3h.008v.008H6.75V18z"/></svg>""",
                Order = 95,
                Section = MenuSection.AdminSidebar,
            }
        );
    }

    public void ConfigureSettings(ISettingsBuilder settings)
    {
        settings
            .Add(
                new SettingDefinition
                {
                    Key = "auditlogs.capture.http",
                    DisplayName = "HTTP Request Capture",
                    Description = "Capture all HTTP requests in audit log",
                    Group = "Audit Logs",
                    Scope = SettingScope.System,
                    DefaultValue = "true",
                    Type = SettingType.Bool,
                }
            )
            .Add(
                new SettingDefinition
                {
                    Key = "auditlogs.capture.domain",
                    DisplayName = "Domain Event Capture",
                    Description = "Capture domain events from the event bus",
                    Group = "Audit Logs",
                    Scope = SettingScope.System,
                    DefaultValue = "true",
                    Type = SettingType.Bool,
                }
            )
            .Add(
                new SettingDefinition
                {
                    Key = "auditlogs.capture.changes",
                    DisplayName = "Entity Change Tracking",
                    Description = "Capture EF Core SaveChanges with property diffs",
                    Group = "Audit Logs",
                    Scope = SettingScope.System,
                    DefaultValue = "true",
                    Type = SettingType.Bool,
                }
            )
            .Add(
                new SettingDefinition
                {
                    Key = "auditlogs.capture.requestbodies",
                    DisplayName = "Request Body Capture",
                    Description = "Store request bodies for POST/PUT/DELETE (redacted)",
                    Group = "Audit Logs",
                    Scope = SettingScope.System,
                    DefaultValue = "true",
                    Type = SettingType.Bool,
                }
            )
            .Add(
                new SettingDefinition
                {
                    Key = "auditlogs.capture.querystrings",
                    DisplayName = "Query String Capture",
                    Description = "Store URL query strings",
                    Group = "Audit Logs",
                    Scope = SettingScope.System,
                    DefaultValue = "true",
                    Type = SettingType.Bool,
                }
            )
            .Add(
                new SettingDefinition
                {
                    Key = "auditlogs.capture.useragent",
                    DisplayName = "User Agent Capture",
                    Description = "Store browser/client user agent strings",
                    Group = "Audit Logs",
                    Scope = SettingScope.System,
                    DefaultValue = "false",
                    Type = SettingType.Bool,
                }
            )
            .Add(
                new SettingDefinition
                {
                    Key = "auditlogs.retention.enabled",
                    DisplayName = "Auto-Cleanup",
                    Description = "Automatically delete old audit entries",
                    Group = "Audit Logs",
                    Scope = SettingScope.System,
                    DefaultValue = "true",
                    Type = SettingType.Bool,
                }
            )
            .Add(
                new SettingDefinition
                {
                    Key = "auditlogs.retention.days",
                    DisplayName = "Retention Days",
                    Description = "Number of days to keep audit entries",
                    Group = "Audit Logs",
                    Scope = SettingScope.System,
                    DefaultValue = "90",
                    Type = SettingType.Number,
                }
            )
            .Add(
                new SettingDefinition
                {
                    Key = "auditlogs.excluded.paths",
                    DisplayName = "Excluded Paths",
                    Description = "Comma-separated path prefixes to skip (e.g., /health,/metrics)",
                    Group = "Audit Logs",
                    Scope = SettingScope.System,
                    DefaultValue = "/health,/metrics,/_content,/js/,/css/",
                    Type = SettingType.Text,
                }
            );
    }
}
