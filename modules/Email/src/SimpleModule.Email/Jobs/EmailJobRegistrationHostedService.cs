using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SimpleModule.BackgroundJobs.Contracts;

namespace SimpleModule.Email.Jobs;

public partial class EmailJobRegistrationHostedService(
    IBackgroundJobs backgroundJobs,
    IOptions<EmailModuleOptions> options,
    ILogger<EmailJobRegistrationHostedService> logger
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var cron = options.Value.RetryIntervalCron;
            await backgroundJobs.AddRecurringAsync<RetryFailedEmailsJob>(
                "email-retry-failed",
                cron,
                ct: cancellationToken
            );
        }
        catch (InvalidOperationException ex)
        {
            LogRegistrationFailed(logger, ex);
        }
        catch (FormatException ex)
        {
            LogRegistrationFailed(logger, ex);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to register email retry recurring job. Check the RetryIntervalCron setting."
    )]
    private static partial void LogRegistrationFailed(ILogger logger, Exception ex);
}
