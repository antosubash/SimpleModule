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
using SimpleModule.Core.Settings;
using SimpleModule.Database;
using SimpleModule.Settings.Contracts;

namespace SimpleModule.AuditLogs;

[Module(
    AuditLogsConstants.ModuleName,
    RoutePrefix = AuditLogsConstants.RoutePrefix,
    ViewPrefix = AuditLogsConstants.ViewPrefix
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

    // Menu items removed — accessible via Admin hub page

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
