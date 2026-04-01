using FluentAssertions;
using SimpleModule.BackgroundJobs.Contracts;

namespace BackgroundJobs.Tests.Unit;

public sealed class JobFilterTests
{
    [Fact]
    public void DefaultFilter_HasCorrectDefaults()
    {
        var filter = new JobFilter();

        filter.State.Should().BeNull();
        filter.JobType.Should().BeNull();
        filter.Page.Should().Be(1);
        filter.PageSize.Should().Be(20);
    }

    [Fact]
    public void Filter_CanSetAllProperties()
    {
        var filter = new JobFilter
        {
            State = JobState.Failed,
            JobType = "ImportJob",
            Page = 3,
            PageSize = 50,
        };

        filter.State.Should().Be(JobState.Failed);
        filter.JobType.Should().Be("ImportJob");
        filter.Page.Should().Be(3);
        filter.PageSize.Should().Be(50);
    }
}
