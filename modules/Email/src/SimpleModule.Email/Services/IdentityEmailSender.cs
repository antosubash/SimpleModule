using Microsoft.AspNetCore.Identity;
using SimpleModule.Email.Contracts;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Email.Services;

public class IdentityEmailSender(IEmailContracts emailContracts) : IEmailSender<ApplicationUser>
{
    public async Task SendConfirmationLinkAsync(
        ApplicationUser user,
        string email,
        string confirmationLink
    )
    {
        await emailContracts.SendEmailAsync(
            new SendEmailRequest
            {
                To = email,
                Subject = "Confirm your email",
                Body =
                    $"""Please confirm your account by <a href="{confirmationLink}">clicking here</a>.""",
                IsHtml = true,
            }
        );
    }

    public async Task SendPasswordResetLinkAsync(
        ApplicationUser user,
        string email,
        string resetLink
    )
    {
        await emailContracts.SendEmailAsync(
            new SendEmailRequest
            {
                To = email,
                Subject = "Reset your password",
                Body =
                    $"""Please reset your password by <a href="{resetLink}">clicking here</a>.""",
                IsHtml = true,
            }
        );
    }

    public async Task SendPasswordResetCodeAsync(
        ApplicationUser user,
        string email,
        string resetCode
    )
    {
        await emailContracts.SendEmailAsync(
            new SendEmailRequest
            {
                To = email,
                Subject = "Reset your password",
                Body = $"Please reset your password using the following code: {resetCode}",
                IsHtml = false,
            }
        );
    }
}
