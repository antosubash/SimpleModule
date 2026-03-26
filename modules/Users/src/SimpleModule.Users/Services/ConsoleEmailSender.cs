using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using SimpleModule.Users.Entities;

namespace SimpleModule.Users.Services;

public partial class ConsoleEmailSender(ILogger<ConsoleEmailSender> logger)
    : IEmailSender<ApplicationUser>
{
    public Task SendConfirmationLinkAsync(
        ApplicationUser user,
        string email,
        string confirmationLink
    )
    {
        LogConfirmationLink(logger, email, confirmationLink);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
    {
        LogPasswordResetLink(logger, email, resetLink);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
    {
        LogPasswordResetCode(logger, email, resetCode);
        return Task.CompletedTask;
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Email confirmation for {Email}: {Link}"
    )]
    private static partial void LogConfirmationLink(ILogger logger, string email, string link);

    [LoggerMessage(Level = LogLevel.Information, Message = "Password reset for {Email}: {Link}")]
    private static partial void LogPasswordResetLink(ILogger logger, string email, string link);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Password reset code for {Email}: {Code}"
    )]
    private static partial void LogPasswordResetCode(ILogger logger, string email, string code);
}
