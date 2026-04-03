using Microsoft.Extensions.Logging;

namespace SimpleModule.Email.Providers;

public partial class LogEmailProvider(ILogger<LogEmailProvider> logger) : IEmailProvider
{
    public string Name => "Log";

    public Task SendAsync(EmailEnvelope envelope, CancellationToken cancellationToken = default)
    {
        LogEmail(logger, envelope.From, envelope.To, envelope.Subject, envelope.Body);
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
