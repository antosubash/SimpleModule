namespace SimpleModule.Core.Authorization.Policies;

/// <summary>
/// Options for <see cref="IAuthorizer"/>. By default a denied "view" action surfaces as
/// 404 instead of 403 so unauthorized callers cannot enumerate resource IDs; add or
/// remove actions to tune that behavior per host.
/// </summary>
public sealed class PolicyAuthorizationOptions
{
    /// <summary>
    /// Actions whose denial throws <see cref="Exceptions.NotFoundException"/> (404)
    /// instead of <see cref="Exceptions.ForbiddenException"/> (403). Case-insensitive.
    /// </summary>
    public ISet<string> NotFoundActions { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { PolicyActions.View };
}
