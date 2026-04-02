using FluentAssertions;
using SimpleModule.BackgroundJobs.Contracts;
using SimpleModule.BackgroundJobs.Services;
using TickerQ.Utilities.Enums;

namespace BackgroundJobs.Tests.Unit;

public sealed class MapTickerStatusTests
{
    [Theory]
    [InlineData(TickerStatus.Idle, JobState.Pending)]
    [InlineData(TickerStatus.Queued, JobState.Pending)]
    [InlineData(TickerStatus.InProgress, JobState.Running)]
    [InlineData(TickerStatus.Done, JobState.Completed)]
    [InlineData(TickerStatus.DueDone, JobState.Completed)]
    [InlineData(TickerStatus.Failed, JobState.Failed)]
    [InlineData(TickerStatus.Cancelled, JobState.Cancelled)]
    [InlineData(TickerStatus.Skipped, JobState.Skipped)]
    public void MapTickerStatus_MapsCorrectly(TickerStatus input, JobState expected)
    {
        var result = BackgroundJobsService.MapTickerStatus(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void MapTickerStatus_UnknownValue_DefaultsToPending()
    {
        var result = BackgroundJobsService.MapTickerStatus((TickerStatus)999);

        result.Should().Be(JobState.Pending);
    }
}
