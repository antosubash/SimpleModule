namespace SimpleModule.Email.Providers;

public interface IEmailProvider
{
    string Name { get; }
    Task SendAsync(
        string from,
        string fromName,
        string to,
        string? cc,
        string? bcc,
        string subject,
        string body,
        bool isHtml,
        CancellationToken cancellationToken = default
    );
}
