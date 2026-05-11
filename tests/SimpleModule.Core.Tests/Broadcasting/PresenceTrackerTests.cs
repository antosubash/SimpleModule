using FluentAssertions;
using SimpleModule.Core.Broadcasting;
using SimpleModule.Hosting.Broadcasting;

namespace SimpleModule.Core.Tests.Broadcasting;

public class PresenceTrackerTests
{
    [Fact]
    public void Add_First_Connection_Returns_Joined_True()
    {
        var tracker = new PresenceTracker();
        var member = new PresenceMember("u1");

        var joined = tracker.Add("presence-room", "c1", member);

        joined.Should().BeTrue();
        tracker.Members("presence-room").Should().ContainSingle().Which.UserId.Should().Be("u1");
    }

    [Fact]
    public void Add_Second_Connection_For_Same_User_Does_Not_Refire_Join()
    {
        var tracker = new PresenceTracker();
        tracker.Add("presence-room", "c1", new PresenceMember("u1"));

        var joined = tracker.Add("presence-room", "c2", new PresenceMember("u1"));

        joined.Should().BeFalse();
        tracker.Members("presence-room").Should().ContainSingle();
    }

    [Fact]
    public void Remove_Last_Connection_For_User_Reports_Departure()
    {
        var tracker = new PresenceTracker();
        tracker.Add("presence-room", "c1", new PresenceMember("u1"));

        var left = tracker.Remove("presence-room", "c1", out var member);

        left.Should().BeTrue();
        member!.UserId.Should().Be("u1");
        tracker.Members("presence-room").Should().BeEmpty();
    }

    [Fact]
    public void Remove_With_Other_Connections_For_Same_User_Does_Not_Report_Departure()
    {
        var tracker = new PresenceTracker();
        tracker.Add("presence-room", "c1", new PresenceMember("u1"));
        tracker.Add("presence-room", "c2", new PresenceMember("u1"));

        var left = tracker.Remove("presence-room", "c1", out _);

        left.Should().BeFalse();
        tracker.Members("presence-room").Should().ContainSingle();
    }

    [Fact]
    public void RemoveConnection_Returns_Channels_With_Departed_Last_User()
    {
        var tracker = new PresenceTracker();
        tracker.Add("presence-a", "c1", new PresenceMember("u1"));
        tracker.Add("presence-a", "c2", new PresenceMember("u2"));
        tracker.Add("presence-b", "c1", new PresenceMember("u1"));

        var departures = tracker.RemoveConnection("c1");

        departures
            .Should()
            .HaveCount(2)
            .And.Contain(d => d.Channel == "presence-a" && d.Member.UserId == "u1")
            .And.Contain(d => d.Channel == "presence-b" && d.Member.UserId == "u1");
    }
}
