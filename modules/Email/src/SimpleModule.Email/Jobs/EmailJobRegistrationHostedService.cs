using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SimpleModule.BackgroundJobs.Contracts;

namespace SimpleModule.Email.Jobs;

public class EmailJobRegistrationHostedService(
    IBackgroundJobs backgroundJobs,
    IOptions<EmailModuleOptions> options
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var cron = options.Value.RetryIntervalCron;
        await backgroundJobs.AddRecurringAsync<RetryFailedEmailsJob>(
            "email-retry-failed",
            cron,
            ct: cancellationToken
        );
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
