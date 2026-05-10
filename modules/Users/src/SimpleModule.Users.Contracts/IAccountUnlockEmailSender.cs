namespace SimpleModule.Users.Contracts;

public interface IAccountUnlockEmailSender
{
    Task SendUnlockLinkAsync(string email, string unlockLink);
}
