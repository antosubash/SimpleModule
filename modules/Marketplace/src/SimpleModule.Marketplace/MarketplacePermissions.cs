using SimpleModule.Core.Authorization;

namespace SimpleModule.Marketplace;

public sealed class MarketplacePermissions : IModulePermissions
{
    public const string View = "Marketplace.View";
    public const string Install = "Marketplace.Install";
    public const string Uninstall = "Marketplace.Uninstall";
}
