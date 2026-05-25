using SimpleModule.Identity.Contracts;

namespace SimpleModule.OpenIddict.Contracts;

public static class AuthConstants
{
    public const string OAuth2Scheme = "oauth2";
    public const string SmartAuthPolicy = IdentityAuthConstants.SmartAuthPolicy;
    public const string OpenIdScope = "openid";
    public const string ProfileScope = "profile";
    public const string EmailScope = "email";
    public const string RolesScope = "roles";
}
