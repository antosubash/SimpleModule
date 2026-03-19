using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using SimpleModule.Core.Authorization;

namespace SimpleModule.Core.Tests.Authorization;

public class PermissionAuthorizationHandlerTests
{
    [Fact]
    public async Task Handle_UserWithPermissionClaim_Succeeds()
    {
        var handler = new PermissionAuthorizationHandler();
        var requirement = new PermissionRequirement("Products.View");
        var user = CreateUser(new Claim("permission", "Products.View"));
        var context = new AuthorizationHandlerContext([requirement], user, null);

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_UserWithoutPermissionClaim_Fails()
    {
        var handler = new PermissionAuthorizationHandler();
        var requirement = new PermissionRequirement("Products.View");
        var user = CreateUser(new Claim("permission", "Orders.View"));
        var context = new AuthorizationHandlerContext([requirement], user, null);

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_AdminRole_BypassesPermissionCheck()
    {
        var handler = new PermissionAuthorizationHandler();
        var requirement = new PermissionRequirement("Products.Delete");
        var user = CreateUser(new Claim(ClaimTypes.Role, "Admin"));
        var context = new AuthorizationHandlerContext([requirement], user, null);

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    private static ClaimsPrincipal CreateUser(params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, "Test");
        return new ClaimsPrincipal(identity);
    }
}
