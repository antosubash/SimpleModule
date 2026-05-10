using Microsoft.Extensions.Logging;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Users.Services;

public partial class ConsoleAccountUnlockEmailSender(
    ILogger<ConsoleAccountUnlockEmailSender> logger
) : IAccountUnlockEmailSender
{
    public Task SendUnlockLinkAsync(string email, string unlockLink)
    {
        LogUnlockLink(logger, email, unlockLink);
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Account unlock for {Email}: {Link}")]
    private static partial void LogUnlockLink(ILogger logger, string email, string link);
}
