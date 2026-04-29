using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SimpleModule.Core.Extensions;

namespace SimpleModule.Core.Authorization;

/// <summary>
/// Ambient access to the current request's principal, without forcing every
/// caller to inject IHttpContextAccessor and walk claims by hand.
/// </summary>
public interface ICurrentUser
{
    string? Id { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
    bool HasPermission(string permission);
    ClaimsPrincipal? Principal { get; }
}

public sealed class HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public string? Id => Principal?.GetUserId();

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public bool IsInRole(string role) => Principal?.IsInRole(role) == true;

    public bool HasPermission(string permission) =>
        Principal is not null && Principal.HasPermission(permission);
}
