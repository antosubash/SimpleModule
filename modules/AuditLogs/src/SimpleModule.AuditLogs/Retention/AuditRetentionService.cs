using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SimpleModule.AuditLogs.Contracts;
using SimpleModule.Core.Settings;
using SimpleModule.Settings.Contracts;

namespace SimpleModule.AuditLogs.Retention;

public sealed partial class AuditRetentionService(
    IServiceScopeFactory scopeFactory,
    IOptions<AuditLogsModuleOptions> moduleOptions,
    ILogger<AuditRetentionService> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        var checkInterval = moduleOptions.Value.RetentionCheckInterval;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunCleanupAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
#pragma warning disable CA1031
            catch (Exception ex)
#pragma warning restore CA1031
            {
                LogError(logger, ex);
            }

            await Task.Delay(checkInterval, stoppingToken);
        }
    }

    private async Task RunCleanupAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var settings = scope.ServiceProvider.GetService<ISettingsContracts>();
        var auditLogs = scope.ServiceProvider.GetRequiredService<IAuditLogContracts>();

        if (settings is not null)
        {
            var enabled = await settings.GetSettingAsync<bool>(
                "auditlogs.retention.enabled",
                SettingScope.System
            );
            if (enabled == false)
            {
                LogSkipped(logger);
                return;
            }
        }

        var retentionDays = moduleOptions.Value.RetentionDays;
        if (settings is not null)
        {
            var days = await settings.GetSettingAsync<int>(
                "auditlogs.retention.days",
                SettingScope.System
            );
            if (days > 0)
                retentionDays = days;
        }

        var cutoff = DateTimeOffset.UtcNow.AddDays(-retentionDays);
        var purged = await auditLogs.PurgeOlderThanAsync(cutoff);
        LogCompleted(logger, purged, retentionDays);
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Retention cleanup purged {Count} entries older than {Days} days"
    )]
    private static partial void LogCompleted(ILogger logger, int count, int days);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Retention cleanup skipped (disabled)")]
    private static partial void LogSkipped(ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "Retention cleanup failed")]
    private static partial void LogError(ILogger logger, Exception ex);
}
