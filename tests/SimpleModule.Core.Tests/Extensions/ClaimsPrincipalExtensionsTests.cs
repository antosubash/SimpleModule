using System.Security.Claims;
using FluentAssertions;
using SimpleModule.Core.Extensions;

namespace SimpleModule.Core.Tests.Extensions;

public class ClaimsPrincipalExtensionsTests
{
    [Fact]
    public void HasPermission_AdminRole_ReturnsTrue()
    {
        var user = CreateUser(new Claim(ClaimTypes.Role, "Admin"));

        user.HasPermission("Anything.Goes").Should().BeTrue();
    }

    [Fact]
    public void HasPermission_ExactClaimMatch_ReturnsTrue()
    {
        var user = CreateUser(new Claim("permission", "Products.View"));

        user.HasPermission("Products.View").Should().BeTrue();
    }

    [Fact]
    public void HasPermission_WildcardClaimMatch_ReturnsTrue()
    {
        var user = CreateUser(new Claim("permission", "Products.*"));

        user.HasPermission("Products.View").Should().BeTrue();
        user.HasPermission("Products.Delete").Should().BeTrue();
    }

    [Fact]
    public void HasPermission_GlobalWildcardClaim_ReturnsTrue()
    {
        var user = CreateUser(new Claim("permission", "*"));

        user.HasPermission("Foo").Should().BeTrue();
    }

    [Fact]
    public void HasPermission_NoMatchingClaim_ReturnsFalse()
    {
        var user = CreateUser(new Claim("permission", "Orders.View"));

        user.HasPermission("Products.View").Should().BeFalse();
    }

    [Fact]
    public void HasPermission_EmptyPrincipal_ReturnsFalse()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity());

        user.HasPermission("Anything").Should().BeFalse();
    }

    [Fact]
    public void HasPermission_WildcardClaim_DoesNotMatchAcrossModuleBoundary()
    {
        // "Products.*" must not match a permission outside the "Products." prefix.
        var user = CreateUser(new Claim("permission", "Products.*"));

        user.HasPermission("ProductsArchive.View").Should().BeFalse();
        user.HasPermission("Products.View").Should().BeTrue();
    }

    [Fact]
    public void GetUserId_PrefersSubClaim_ForOpenIddictAndKeycloak()
    {
        // OpenIddict/Keycloak with MapInboundClaims=false emit the id as "sub",
        // never ClaimTypes.NameIdentifier.
        var user = CreateUser(new Claim("sub", "keycloak-user-1"));

        user.GetUserId().Should().Be("keycloak-user-1");
    }

    [Fact]
    public void GetUserId_FallsBackToNameIdentifier_ForAspNetIdentity()
    {
        var user = CreateUser(new Claim(ClaimTypes.NameIdentifier, "identity-user-1"));

        user.GetUserId().Should().Be("identity-user-1");
    }

    [Fact]
    public void GetRoles_ReadsBothRoleClaimTypes()
    {
        var user = CreateUser(
            new Claim("role", "editor"),
            new Claim(ClaimTypes.Role, "admin")
        );

        user.GetRoles().Should().BeEquivalentTo("editor", "admin");
    }

    private static ClaimsPrincipal CreateUser(params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, "Test");
        return new ClaimsPrincipal(identity);
    }
}
