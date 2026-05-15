using Microsoft.Extensions.Logging;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Users.Services;

public partial class ConsoleSmsSender(ILogger<ConsoleSmsSender> logger) : ISmsSender
{
    public Task SendVerificationCodeAsync(
        ApplicationUser user,
        string phoneNumber,
        string code,
        CancellationToken cancellationToken = default
    )
    {
        LogVerificationCode(logger, phoneNumber, code);
        return Task.CompletedTask;
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Phone verification code for {PhoneNumber}: {Code}"
    )]
    private static partial void LogVerificationCode(
        ILogger logger,
        string phoneNumber,
        string code
    );
}
