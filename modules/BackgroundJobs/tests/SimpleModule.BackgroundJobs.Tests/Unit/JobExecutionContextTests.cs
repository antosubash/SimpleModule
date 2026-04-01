using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SimpleModule.BackgroundJobs.Contracts;
using SimpleModule.BackgroundJobs.Services;

namespace BackgroundJobs.Tests.Unit;

public sealed class JobExecutionContextTests
{
    [Fact]
    public void GetData_DeserializesPayload()
    {
        var data = new TestData("hello", 42);
        var payload = new JobDispatchPayload("TestType", JsonSerializer.Serialize(data));
        var channel = new ProgressChannel(NullLogger<ProgressChannel>.Instance);
        var context = new JobExecutionContext(JobId.From(Guid.NewGuid()), payload, channel);

        var result = context.GetData<TestData>();

        result.Name.Should().Be("hello");
        result.Value.Should().Be(42);
    }

    [Fact]
    public void GetData_NullData_Throws()
    {
        var payload = new JobDispatchPayload("TestType", null);
        var channel = new ProgressChannel(NullLogger<ProgressChannel>.Instance);
        var context = new JobExecutionContext(JobId.From(Guid.NewGuid()), payload, channel);

        var act = () => context.GetData<TestData>();

        act.Should().Throw<InvalidOperationException>().WithMessage("*No data*");
    }

    [Fact]
    public void GetData_EmptyData_Throws()
    {
        var payload = new JobDispatchPayload("TestType", "");
        var channel = new ProgressChannel(NullLogger<ProgressChannel>.Instance);
        var context = new JobExecutionContext(JobId.From(Guid.NewGuid()), payload, channel);

        var act = () => context.GetData<TestData>();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void GetData_ComplexType_Deserializes()
    {
        var data = new ComplexData([1, 2, 3], new Dictionary<string, string> { ["key"] = "value" });
        var payload = new JobDispatchPayload("TestType", JsonSerializer.Serialize(data));
        var channel = new ProgressChannel(NullLogger<ProgressChannel>.Instance);
        var context = new JobExecutionContext(JobId.From(Guid.NewGuid()), payload, channel);

        var result = context.GetData<ComplexData>();

        result.Numbers.Should().BeEquivalentTo([1, 2, 3]);
        result.Metadata.Should().ContainKey("key").WhoseValue.Should().Be("value");
    }

    [Fact]
    public void ReportProgress_EnqueuesToChannel()
    {
        var channel = new ProgressChannel(NullLogger<ProgressChannel>.Instance);
        var jobId = JobId.From(Guid.NewGuid());
        var context = new JobExecutionContext(jobId, new JobDispatchPayload("Test", null), channel);

        context.ReportProgress(50, "Half done");

        channel.Reader.TryRead(out var entry).Should().BeTrue();
        entry!.JobId.Should().Be(jobId.Value);
        entry.Percentage.Should().Be(50);
        entry.Message.Should().Be("Half done");
        entry.LogMessage.Should().BeNull();
    }

    [Fact]
    public void ReportProgress_WithoutMessage_EnqueuesNullMessage()
    {
        var channel = new ProgressChannel(NullLogger<ProgressChannel>.Instance);
        var context = new JobExecutionContext(
            JobId.From(Guid.NewGuid()),
            new JobDispatchPayload("Test", null),
            channel
        );

        context.ReportProgress(75);

        channel.Reader.TryRead(out var entry).Should().BeTrue();
        entry!.Percentage.Should().Be(75);
        entry.Message.Should().BeNull();
    }

    [Fact]
    public void Log_EnqueuesToChannelWithLogMessage()
    {
        var channel = new ProgressChannel(NullLogger<ProgressChannel>.Instance);
        var jobId = JobId.From(Guid.NewGuid());
        var context = new JobExecutionContext(jobId, new JobDispatchPayload("Test", null), channel);

        context.Log("Something happened");

        channel.Reader.TryRead(out var entry).Should().BeTrue();
        entry!.JobId.Should().Be(jobId.Value);
        entry.LogMessage.Should().Be("Something happened");
        entry.Message.Should().BeNull();
    }

    [Fact]
    public void JobId_ReturnsCorrectId()
    {
        var id = JobId.From(Guid.NewGuid());
        var context = new JobExecutionContext(
            id,
            new JobDispatchPayload("Test", null),
            new ProgressChannel(NullLogger<ProgressChannel>.Instance)
        );

        context.JobId.Should().Be(id);
    }

    [Fact]
    public void ReportProgress_MultipleUpdates_AllEnqueued()
    {
        var channel = new ProgressChannel(NullLogger<ProgressChannel>.Instance);
        var context = new JobExecutionContext(
            JobId.From(Guid.NewGuid()),
            new JobDispatchPayload("Test", null),
            channel
        );

        context.ReportProgress(25, "Quarter");
        context.ReportProgress(50, "Half");
        context.ReportProgress(75, "Three quarters");

        var count = 0;
        while (channel.Reader.TryRead(out _))
        {
            count++;
        }
        count.Should().Be(3);
    }

    private record TestData(string Name, int Value);
    private record ComplexData(List<int> Numbers, Dictionary<string, string> Metadata);
}
