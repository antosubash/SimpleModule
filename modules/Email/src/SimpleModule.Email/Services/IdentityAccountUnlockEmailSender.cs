using SimpleModule.Email.Contracts;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Email.Services;

public class IdentityAccountUnlockEmailSender(IEmailContracts emailContracts)
    : IAccountUnlockEmailSender
{
    public async Task SendUnlockLinkAsync(string email, string unlockLink)
    {
        await emailContracts.SendEmailAsync(
            new SendEmailRequest
            {
                To = email,
                Subject = "Unlock your account",
                Body =
                    $"""Your account has been locked due to multiple failed sign-in attempts. <a href="{unlockLink}">Click here to unlock your account</a>.""",
                IsHtml = true,
            }
        );
    }
}
