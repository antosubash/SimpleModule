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

    private static ClaimsPrincipal CreateUser(params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, "Test");
        return new ClaimsPrincipal(identity);
    }
}
