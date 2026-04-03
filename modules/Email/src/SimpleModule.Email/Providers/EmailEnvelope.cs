namespace SimpleModule.Email.Providers;

public sealed record EmailEnvelope(
    string From,
    string FromName,
    string To,
    string? Cc,
    string? Bcc,
    string? ReplyTo,
    string Subject,
    string Body,
    bool IsHtml
);
