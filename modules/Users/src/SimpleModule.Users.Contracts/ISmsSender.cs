namespace SimpleModule.Users.Contracts;

public interface ISmsSender
{
    Task SendVerificationCodeAsync(
        ApplicationUser user,
        string phoneNumber,
        string code,
        CancellationToken cancellationToken = default
    );
}
