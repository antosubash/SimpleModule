using Microsoft.Extensions.Logging;

namespace SimpleModule.Email.Providers;

public partial class LogEmailProvider(ILogger<LogEmailProvider> logger) : IEmailProvider
{
    public string Name => "Log";

    public Task SendAsync(
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
        LogEmail(logger, from, to, subject, body);
        return Task.CompletedTask;
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Email from {From} to {To} | Subject: {Subject} | Body: {Body}"
    )]
    private static partial void LogEmail(
        ILogger logger,
        string from,
        string to,
        string subject,
        string body
    );
}
