using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace SimpleModule.Email.Providers;

public partial class SmtpEmailProvider(
    IOptions<EmailModuleOptions> options,
    ILogger<SmtpEmailProvider> logger
) : IEmailProvider
{
    public string Name => "SMTP";

    public async Task SendAsync(
        EmailEnvelope envelope,
        CancellationToken cancellationToken = default
    )
    {
        var smtp = options.Value.Smtp;
        using var message = CreateMessage(envelope);

        using var client = new SmtpClient();
        await client.ConnectAsync(smtp.Host, smtp.Port, smtp.UseSsl, cancellationToken);

        if (!string.IsNullOrWhiteSpace(smtp.Username))
        {
            await client.AuthenticateAsync(smtp.Username, smtp.Password, cancellationToken);
        }

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);

        LogEmailSent(logger, envelope.To, envelope.Subject);
    }

    private static MimeMessage CreateMessage(EmailEnvelope envelope)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(envelope.FromName, envelope.From));
        message.To.Add(MailboxAddress.Parse(envelope.To));

        if (!string.IsNullOrWhiteSpace(envelope.Cc))
            message.Cc.Add(MailboxAddress.Parse(envelope.Cc));

        if (!string.IsNullOrWhiteSpace(envelope.Bcc))
            message.Bcc.Add(MailboxAddress.Parse(envelope.Bcc));

        if (!string.IsNullOrWhiteSpace(envelope.ReplyTo))
            message.ReplyTo.Add(MailboxAddress.Parse(envelope.ReplyTo));

        message.Subject = envelope.Subject;
        message.Body = new TextPart(envelope.IsHtml ? "html" : "plain") { Text = envelope.Body };

        return message;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "SMTP email sent to {To}: {Subject}")]
    private static partial void LogEmailSent(ILogger logger, string to, string subject);
}
