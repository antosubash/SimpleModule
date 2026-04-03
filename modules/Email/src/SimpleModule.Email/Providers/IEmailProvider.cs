namespace SimpleModule.Email.Providers;

public interface IEmailProvider
{
    string Name { get; }
    Task SendAsync(EmailEnvelope envelope, CancellationToken cancellationToken = default);
}
