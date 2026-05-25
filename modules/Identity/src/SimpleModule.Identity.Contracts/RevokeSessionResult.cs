namespace SimpleModule.Identity.Contracts;

public enum RevokeSessionResult
{
    Revoked,
    NotFound,
    BlockedCurrent,
}
