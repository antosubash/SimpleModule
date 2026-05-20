using FluentAssertions;
using SimpleModule.BackgroundJobs.Contracts;

namespace SimpleModule.BackgroundJobs.Tests.Scheduler;

public sealed class ScheduledJobBuilderTests
{
    [Fact]
    public void Cron_StoresExpression()
    {
        var registry = new SchedulerRegistry();
        registry.Job<FakeJobA>("a").Cron("0 8 * * MON-FRI");

        var def = registry.Definitions.Single();
        def.CronExpression.Should().Be("0 8 * * MON-FRI");
    }

    [Theory]
    [InlineData(1, "*/1 * * * *")]
    [InlineData(5, "*/5 * * * *")]
    [InlineData(30, "*/30 * * * *")]
    public void EveryMinutes_RendersCron(int minutes, string expected)
    {
        var registry = new SchedulerRegistry();
        registry.Job<FakeJobA>("a").EveryMinutes(minutes);
        registry.Definitions.Single().CronExpression.Should().Be(expected);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(60)]
    public void EveryMinutes_RejectsOutOfRange(int minutes)
    {
        var registry = new SchedulerRegistry();
        var act = () => registry.Job<FakeJobA>("a").EveryMinutes(minutes);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Hourly_Daily_Weekdays_RenderExpectedCron()
    {
        var registry = new SchedulerRegistry();
        registry.Job<FakeJobA>("h").Hourly();
        registry.Job<FakeJobB>("d").Daily();
        registry.Job<FakeJobC>("w").Weekdays();

        registry.Definitions.Single(d => d.Name == "h").CronExpression.Should().Be("0 * * * *");
        registry.Definitions.Single(d => d.Name == "d").CronExpression.Should().Be("0 0 * * *");
        registry
            .Definitions.Single(d => d.Name == "w")
            .CronExpression.Should()
            .Be("0 0 * * MON-FRI");
    }

    [Theory]
    [InlineData("02:00", "0 2 * * *")]
    [InlineData("23:45", "45 23 * * *")]
    public void DailyAt_RendersCron(string time, string expected)
    {
        var registry = new SchedulerRegistry();
        registry.Job<FakeJobA>("a").DailyAt(time);
        registry.Definitions.Single().CronExpression.Should().Be(expected);
    }

    [Theory]
    [InlineData("garbage")]
    [InlineData("25:00")]
    [InlineData("2:00")]
    public void DailyAt_RejectsBadTime(string time)
    {
        var registry = new SchedulerRegistry();
        var act = () => registry.Job<FakeJobA>("a").DailyAt(time);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Timezone_AcceptsKnownZone()
    {
        var registry = new SchedulerRegistry();
        registry.Job<FakeJobA>("a").Timezone("UTC");
        registry.Definitions.Single().TimeZoneId.Should().Be("UTC");
    }

    [Fact]
    public void Timezone_RejectsUnknownZone()
    {
        var registry = new SchedulerRegistry();
        var act = () => registry.Job<FakeJobA>("a").Timezone("Not/A/Zone");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Flags_AreCarriedOnDefinition()
    {
        var registry = new SchedulerRegistry();
        registry
            .Job<FakeJobA>("a")
            .Hourly()
            .WithoutOverlapping()
            .OnOneServer()
            .WithPayload(new { x = 1 });

        var def = registry.Definitions.Single();
        def.WithoutOverlapping.Should().BeTrue();
        def.OnOneServer.Should().BeTrue();
        def.Payload.Should().NotBeNull();
    }

    [Fact]
    public void Job_DuplicateNameThrows()
    {
        var registry = new SchedulerRegistry();
        registry.Job<FakeJobA>("dup").Daily();
        var act = () => registry.Job<FakeJobB>("dup").Daily();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Job_DefaultName_UsesJobTypeFullName()
    {
        var registry = new SchedulerRegistry();
        registry.Job<FakeJobA>().Daily();
        registry.Definitions.Single().Name.Should().Be(typeof(FakeJobA).FullName);
    }
}
