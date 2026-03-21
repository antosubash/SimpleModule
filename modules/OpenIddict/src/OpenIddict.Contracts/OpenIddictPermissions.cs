using SimpleModule.Core.Authorization;

namespace SimpleModule.OpenIddict.Contracts;

public sealed class OpenIddictPermissions : IModulePermissions
{
    public const string ManageClients = "OpenIddict.ManageClients";
}
