using FluentAssertions;
using SimpleModule.BackgroundJobs.Services;

namespace BackgroundJobs.Tests.Unit;

public sealed class ProgressChannelTests
{
    [Fact]
    public async Task Enqueue_WritesToChannel_CanRead()
    {
        var channel = new ProgressChannel();
        var entry = new ProgressEntry(Guid.NewGuid(), 50, "Half done", null, DateTimeOffset.UtcNow);

        channel.Enqueue(entry);

        var canRead = await channel.Reader.WaitToReadAsync(
            new CancellationTokenSource(TimeSpan.FromSeconds(1)).Token
        );
        canRead.Should().BeTrue();

        channel.Reader.TryRead(out var result).Should().BeTrue();
        result.Should().Be(entry);
    }

    [Fact]
    public void Enqueue_MultipleEntries_AllReadable()
    {
        var channel = new ProgressChannel();

        for (var i = 0; i < 10; i++)
        {
            channel.Enqueue(
                new ProgressEntry(Guid.NewGuid(), i * 10, $"Step {i}", null, DateTimeOffset.UtcNow)
            );
        }

        var count = 0;
        while (channel.Reader.TryRead(out _))
        {
            count++;
        }

        count.Should().Be(10);
    }

    [Fact]
    public void Enqueue_PreservesEntryData()
    {
        var channel = new ProgressChannel();
        var jobId = Guid.NewGuid();
        var timestamp = DateTimeOffset.UtcNow;
        var entry = new ProgressEntry(jobId, 75, "Processing", "Log message", timestamp);

        channel.Enqueue(entry);

        channel.Reader.TryRead(out var result).Should().BeTrue();
        result!.JobId.Should().Be(jobId);
        result.Percentage.Should().Be(75);
        result.Message.Should().Be("Processing");
        result.LogMessage.Should().Be("Log message");
        result.Timestamp.Should().Be(timestamp);
    }

    [Fact]
    public void TryRead_EmptyChannel_ReturnsFalse()
    {
        var channel = new ProgressChannel();

        channel.Reader.TryRead(out var result).Should().BeFalse();
        result.Should().BeNull();
    }
}
