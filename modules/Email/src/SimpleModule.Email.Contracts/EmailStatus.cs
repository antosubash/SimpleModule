namespace SimpleModule.Email.Contracts;

public enum EmailStatus
{
    Queued,
    Sending,
    Sent,
    Failed,
    Retrying,
}
