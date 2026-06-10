namespace SimpleModule.Core.Authorization.Policies;

/// <summary>
/// Host-level options for <see cref="IAuthorizer"/>. The preferred way to surface a
/// denial as 404 is <see cref="AuthorizationResult.DenyAsNotFound"/> — the policy knows
/// whether a resource's existence is secret, and the decision travels with it. This
/// option is a blunt host-wide override on top of that.
/// </summary>
public sealed class PolicyAuthorizationOptions
{
    /// <summary>
    /// Actions whose denial always throws <see cref="Exceptions.NotFoundException"/>
    /// (404) instead of <see cref="Exceptions.ForbiddenException"/> (403), regardless of
    /// the policy's decision — including an explicit <c>Deny(reason)</c>, whose reason is
    /// then swallowed. Empty by default; action names are not namespaced, so an entry
    /// applies to every module in the host. Configure only at the host level — modules
    /// must not mutate this set.
    /// </summary>
    public ISet<string> NotFoundActions { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);
}
