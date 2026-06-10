namespace SimpleModule.Core.Authorization.Policies;

/// <summary>
/// Outcome of a policy check. Use <see cref="Allow"/> / <see cref="Deny"/> to construct.
/// </summary>
public sealed class AuthorizationResult
{
    private static readonly AuthorizationResult AllowedResult = new(true, null);

    private AuthorizationResult(bool isAllowed, string? reason)
    {
        IsAllowed = isAllowed;
        Reason = reason;
    }

    public bool IsAllowed { get; }

    /// <summary>
    /// Optional human-readable denial reason, surfaced verbatim in the 403 response
    /// detail. Write it for the end user — never include internal identifiers or
    /// implementation details.
    /// </summary>
    public string? Reason { get; }

    public static AuthorizationResult Allow() => AllowedResult;

    public static AuthorizationResult Deny(string? reason = null) => new(false, reason);
}
