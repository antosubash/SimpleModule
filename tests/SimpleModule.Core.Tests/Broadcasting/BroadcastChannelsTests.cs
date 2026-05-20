using FluentAssertions;
using SimpleModule.Core.Broadcasting;

namespace SimpleModule.Core.Tests.Broadcasting;

public class BroadcastChannelsTests
{
    [Fact]
    public void ForUser_Produces_Private_User_Channel()
    {
        BroadcastChannels.ForUser("abc").Should().Be("private-users.abc");
    }

    [Fact]
    public void ForTenant_Produces_Private_Tenant_Channel()
    {
        BroadcastChannels.ForTenant("t1").Should().Be("private-tenants.t1");
    }

    [Theory]
    [InlineData("private-foo", true)]
    [InlineData("presence-room", true)]
    [InlineData("public-thing", false)]
    [InlineData("orders", false)]
    public void IsPrivate_Matches_Reserved_Prefixes(string channel, bool expected)
    {
        BroadcastChannels.IsPrivate(channel).Should().Be(expected);
    }

    [Theory]
    [InlineData("presence-room", true)]
    [InlineData("private-x", false)]
    public void IsPresence_Detects_Presence_Channels(string channel, bool expected)
    {
        BroadcastChannels.IsPresence(channel).Should().Be(expected);
    }
}
