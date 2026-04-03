using SimpleModule.Email.Contracts;

namespace SimpleModule.Email.Jobs;

public sealed record SendEmailJobData(EmailMessageId MessageId);
