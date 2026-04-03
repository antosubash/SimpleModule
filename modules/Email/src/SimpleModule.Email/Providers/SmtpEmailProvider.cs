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
        string from,
        string fromName,
        string to,
        string? cc,
        string? bcc,
        string subject,
        string body,
        bool isHtml,
        CancellationToken cancellationToken = default
    )
    {
        var smtp = options.Value.Smtp;
        using var message = CreateMessage(from, fromName, to, cc, bcc, subject, body, isHtml);

        using var client = new SmtpClient();
        await client.ConnectAsync(smtp.Host, smtp.Port, smtp.UseSsl, cancellationToken);

        if (!string.IsNullOrWhiteSpace(smtp.Username))
        {
            await client.AuthenticateAsync(smtp.Username, smtp.Password, cancellationToken);
        }

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);

        LogEmailSent(logger, to, subject);
    }

    private static MimeMessage CreateMessage(
        string from,
        string fromName,
        string to,
        string? cc,
        string? bcc,
        string subject,
        string body,
        bool isHtml
    )
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, from));
        message.To.Add(MailboxAddress.Parse(to));

        if (!string.IsNullOrWhiteSpace(cc))
        {
            message.Cc.Add(MailboxAddress.Parse(cc));
        }

        if (!string.IsNullOrWhiteSpace(bcc))
        {
            message.Bcc.Add(MailboxAddress.Parse(bcc));
        }

        message.Subject = subject;
        message.Body = new TextPart(isHtml ? "html" : "plain") { Text = body };

        return message;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "SMTP email sent to {To}: {Subject}")]
    private static partial void LogEmailSent(ILogger logger, string to, string subject);
}
