using System.Security.Claims;
using FluentAssertions;
using SimpleModule.Core.Broadcasting;
using SimpleModule.Hosting.Broadcasting;

namespace SimpleModule.Core.Tests.Broadcasting;

public class BroadcastAuthorizerChainTests
{
    private static ClaimsPrincipal User(string userId, string? tenantId = null)
    {
        var claims = new List<Claim> { new("sub", userId) };
        if (tenantId is not null)
        {
            claims.Add(new("tenantid", tenantId));
        }
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }

    private static BroadcastContext Context(ClaimsPrincipal? principal, string? tenantId = null) =>
        new(principal, tenantId);

    [Fact]
    public async Task Authenticated_User_May_Subscribe_To_Their_Own_Channel()
    {
        var chain = new BroadcastAuthorizerChain([
            new DefaultBroadcastAuthorizer(),
            new UserChannelAuthorizer(),
            new TenantChannelAuthorizer(),
        ]);

        var allowed = await chain.AuthorizeAsync(
            "private-users.abc",
            Context(User("abc")),
            TestContext.Current.CancellationToken
        );

        allowed.Should().BeTrue();
    }

    [Fact]
    public async Task User_Cannot_Subscribe_To_Other_Users_Private_Channel()
    {
        var chain = new BroadcastAuthorizerChain([
            new DefaultBroadcastAuthorizer(),
            new UserChannelAuthorizer(),
        ]);

        var allowed = await chain.AuthorizeAsync(
            "private-users.victim",
            Context(User("attacker")),
            TestContext.Current.CancellationToken
        );

        allowed.Should().BeFalse();
    }

    [Fact]
    public async Task Tenant_Channel_Requires_Matching_Tenant_Claim()
    {
        var chain = new BroadcastAuthorizerChain([
            new DefaultBroadcastAuthorizer(),
            new TenantChannelAuthorizer(),
        ]);

        var ownTenant = await chain.AuthorizeAsync(
            "private-tenants.t1.orders",
            Context(User("u1", tenantId: "t1"), tenantId: "t1"),
            TestContext.Current.CancellationToken
        );
        var otherTenant = await chain.AuthorizeAsync(
            "private-tenants.t1.orders",
            Context(User("u1", tenantId: "t2"), tenantId: "t2"),
            TestContext.Current.CancellationToken
        );

        ownTenant.Should().BeTrue();
        otherTenant.Should().BeFalse();
    }

    [Fact]
    public async Task Anonymous_Connection_Cannot_Subscribe_To_Private_Channels()
    {
        var chain = new BroadcastAuthorizerChain([new DefaultBroadcastAuthorizer()]);

        var allowed = await chain.AuthorizeAsync(
            "private-users.abc",
            Context(null),
            TestContext.Current.CancellationToken
        );

        allowed.Should().BeFalse();
    }

    [Fact]
    public async Task Custom_Authorizer_Wins_Over_Framework_Default_By_Prefix_Length()
    {
        var chain = new BroadcastAuthorizerChain([
            new DefaultBroadcastAuthorizer(),
            new AllowAllOrders(),
        ]);

        var allowed = await chain.AuthorizeAsync(
            "private-tenants.t1.orders",
            Context(User("u1")),
            TestContext.Current.CancellationToken
        );

        allowed.Should().BeTrue();
    }

    private sealed class AllowAllOrders : IBroadcastChannelAuthorizer
    {
        public string ChannelPrefix => "private-tenants.t1.orders";

        public Task<bool> AuthorizeAsync(
            string channel,
            IBroadcastContext context,
            CancellationToken cancellationToken
        ) => Task.FromResult(true);
    }
}
