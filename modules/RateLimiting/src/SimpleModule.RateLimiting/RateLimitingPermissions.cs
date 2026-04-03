using SimpleModule.Core.Authorization;

namespace SimpleModule.RateLimiting;

public sealed class RateLimitingPermissions : IModulePermissions
{
    public const string View = "RateLimiting.View";
    public const string Create = "RateLimiting.Create";
    public const string Update = "RateLimiting.Update";
    public const string Delete = "RateLimiting.Delete";
}
