namespace SimpleModule.Core.Authorization.Policies;

/// <summary>
/// Outcome of a policy check. Use <see cref="Allow"/>, <see cref="Deny"/>, or
/// <see cref="DenyAsNotFound"/> to construct.
/// </summary>
public sealed class AuthorizationResult
{
    private static readonly AuthorizationResult AllowedResult = new(true, null, false);

    private AuthorizationResult(bool isAllowed, string? reason, bool treatAsNotFound)
    {
        IsAllowed = isAllowed;
        Reason = reason;
        TreatAsNotFound = treatAsNotFound;
    }

    public bool IsAllowed { get; }

    /// <summary>
    /// Optional human-readable denial reason, surfaced verbatim in the 403 response
    /// detail. Write it for the end user — never include internal identifiers or
    /// implementation details.
    /// </summary>
    public string? Reason { get; }

    /// <summary>
    /// When true, <see cref="IAuthorizer.AuthorizeAsync"/> surfaces this denial as 404
    /// instead of 403, hiding the resource's existence from unauthorized callers.
    /// </summary>
    public bool TreatAsNotFound { get; }

    public static AuthorizationResult Allow() => AllowedResult;

    public static AuthorizationResult Deny(string? reason = null) => new(false, reason, false);

    /// <summary>
    /// Denies and asks the authorizer to respond 404 instead of 403 — use when the
    /// caller must not learn whether the resource exists (anti-enumeration). The
    /// <paramref name="reason"/> is kept for <see cref="IAuthorizer.CheckAsync"/>
    /// consumers but never reaches the HTTP response.
    /// </summary>
    public static AuthorizationResult DenyAsNotFound(string? reason = null) =>
        new(false, reason, true);
}
