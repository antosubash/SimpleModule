using FluentAssertions;
using SimpleModule.BackgroundJobs.Contracts;
using SimpleModule.BackgroundJobs.Services;

namespace BackgroundJobs.Tests.Unit;

public sealed class MapQueueStateTests
{
    [Theory]
    [InlineData(JobQueueEntryState.Pending, JobState.Pending)]
    [InlineData(JobQueueEntryState.Claimed, JobState.Running)]
    [InlineData(JobQueueEntryState.Completed, JobState.Completed)]
    [InlineData(JobQueueEntryState.Failed, JobState.Failed)]
    public void MapQueueState_MapsCorrectly(JobQueueEntryState input, JobState expected)
    {
        var result = BackgroundJobsService.MapQueueState(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void MapQueueState_UnknownValue_DefaultsToPending()
    {
        var result = BackgroundJobsService.MapQueueState((JobQueueEntryState)999);

        result.Should().Be(JobState.Pending);
    }
}
