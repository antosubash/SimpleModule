using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SimpleModule.BackgroundJobs.Contracts;
using SimpleModule.Email.Contracts;
using SimpleModule.Email.Contracts.Events;
using SimpleModule.Email.Providers;
using Wolverine;

namespace SimpleModule.Email.Jobs;

public partial class SendEmailJob(
    EmailDbContext db,
    IEmailProvider emailProvider,
    IOptions<EmailModuleOptions> options,
    IMessageBus bus,
    ILogger<SendEmailJob> logger
) : IModuleJob
{
    public async Task ExecuteAsync(
        IJobExecutionContext context,
        CancellationToken cancellationToken
    )
    {
        var jobData = context.GetData<SendEmailJobData>();
        var message = await db.EmailMessages.FindAsync([jobData.MessageId], cancellationToken);
        if (message is null)
        {
            context.Log($"Email message {jobData.MessageId} not found, skipping.");
            return;
        }

        var opts = options.Value;
        var envelope = new EmailEnvelope(
            opts.DefaultFromAddress,
            opts.DefaultFromName,
            message.To,
            message.Cc,
            message.Bcc,
            message.ReplyTo,
            message.Subject,
            message.Body,
            message.IsHtml
        );

        try
        {
            context.Log($"Sending email {message.Id} to {message.To}");
            await emailProvider.SendAsync(envelope, cancellationToken);
            message.Status = EmailStatus.Sent;
            message.SentAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);

            LogEmailSent(logger, message.Id, message.To);
            await bus.PublishAsync(new EmailSentEvent(message.Id, message.To, message.Subject));
        }
        catch (Exception ex)
            when (ex
                    is InvalidOperationException
                        or System.Net.Sockets.SocketException
                        or IOException
                        or MailKit.Net.Smtp.SmtpCommandException
                        or MailKit.Security.AuthenticationException
                        or MailKit.Security.SslHandshakeException
            )
        {
            message.Status = EmailStatus.Failed;
            message.ErrorMessage = ex.Message;
            await db.SaveChangesAsync(cancellationToken);

            LogEmailFailed(logger, message.Id, message.To, ex);
            await bus.PublishAsync(
                new EmailFailedEvent(message.Id, message.To, message.Subject, ex.Message)
            );
        }

        context.ReportProgress(100);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Email {MessageId} sent to {To}")]
    private static partial void LogEmailSent(ILogger logger, EmailMessageId messageId, string to);

    [LoggerMessage(Level = LogLevel.Error, Message = "Email {MessageId} failed to send to {To}")]
    private static partial void LogEmailFailed(
        ILogger logger,
        EmailMessageId messageId,
        string to,
        Exception ex
    );
}
