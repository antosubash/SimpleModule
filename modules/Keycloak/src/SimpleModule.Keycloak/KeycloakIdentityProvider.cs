using SimpleModule.Identity.Contracts;

namespace SimpleModule.Keycloak;

/// <summary>
/// Registers Keycloak as the active identity provider. Keycloak manages users
/// externally, so <see cref="SupportsLocalUsers"/> returns <c>false</c> —
/// registration and password-management pages should be hidden when this
/// provider is active.
/// </summary>
public sealed class KeycloakIdentityProvider : IIdentityProvider
{
    public string Name => "Keycloak";
    public bool SupportsLocalUsers => false;
}
