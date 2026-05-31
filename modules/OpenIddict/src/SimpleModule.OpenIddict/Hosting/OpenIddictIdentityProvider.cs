using SimpleModule.Identity.Contracts;

namespace SimpleModule.OpenIddict.Hosting;

/// <summary>
/// Registers OpenIddict as the active identity provider. Exposes metadata
/// consumed by provider-agnostic infrastructure (e.g. menu items, feature
/// gates) without a hard dependency on OpenIddict internals.
/// </summary>
public sealed class OpenIddictIdentityProvider : IIdentityProvider
{
    public string Name => "OpenIddict";
    public bool SupportsLocalUsers => true;
}
