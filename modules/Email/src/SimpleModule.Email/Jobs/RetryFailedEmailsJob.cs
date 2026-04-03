using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SimpleModule.BackgroundJobs.Contracts;
using SimpleModule.Core.Events;
using SimpleModule.Email.Contracts;
using SimpleModule.Email.Contracts.Events;

namespace SimpleModule.Email.Jobs;

public partial class RetryFailedEmailsJob(
    EmailDbContext db,
    IBackgroundJobs backgroundJobs,
    IOptions<EmailModuleOptions> options,
    IEventBus eventBus,
    ILogger<RetryFailedEmailsJob> logger
) : IModuleJob
{
    private const int BatchSize = 50;

    public async Task ExecuteAsync(
        IJobExecutionContext context,
        CancellationToken cancellationToken
    )
    {
        var maxRetries = options.Value.MaxRetryCount;
        var failedMessages = await db
            .EmailMessages.Where(m => m.Status == EmailStatus.Failed && m.RetryCount < maxRetries)
            .OrderBy(m => m.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        context.Log($"Found {failedMessages.Count} failed emails to retry");

        foreach (var message in failedMessages)
        {
            message.RetryCount++;
            message.Status = EmailStatus.Retrying;
            message.ErrorMessage = null;
        }

        await db.SaveChangesAsync(cancellationToken);

        foreach (var message in failedMessages)
        {
            LogRetryAttempt(logger, message.Id, message.To, message.RetryCount);
            eventBus.PublishInBackground(
                new EmailRetryAttemptEvent(message.Id, message.To, message.RetryCount)
            );

            await backgroundJobs.EnqueueAsync<SendEmailJob>(
                new SendEmailJobData(message.Id),
                cancellationToken
            );
        }

        context.ReportProgress(100, $"Enqueued {failedMessages.Count} retries");
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Retrying email {MessageId} to {To} (attempt {RetryCount})"
    )]
    private static partial void LogRetryAttempt(
        ILogger logger,
        EmailMessageId messageId,
        string to,
        int retryCount
    );
}
