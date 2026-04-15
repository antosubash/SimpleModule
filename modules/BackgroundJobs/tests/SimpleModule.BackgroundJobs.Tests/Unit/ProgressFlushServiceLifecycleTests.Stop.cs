using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SimpleModule.BackgroundJobs.Contracts;
using SimpleModule.BackgroundJobs.Services;

namespace BackgroundJobs.Tests.Unit;

public sealed partial class ProgressFlushServiceLifecycleTests
{
    [Fact]
    public async Task Stop_DrainsRemainingEntriesBeforeExiting()
    {
        var db = _factory.Create();
        var jobId = Guid.NewGuid();
        db.JobProgress.Add(CreateProgress(jobId));
        await db.SaveChangesAsync();

        var channel = new ProgressChannel(NullLogger<ProgressChannel>.Instance);
        // Use a large flush interval so the batch timer won't flush — only the drain on stop
        var service = CreateService(channel, flushIntervalMs: 30_000);
        await service.StartAsync(CancellationToken.None);

        // Enqueue entries
        channel.Enqueue(new ProgressEntry(jobId, 90, "Almost done", null, DateTimeOffset.UtcNow));
        channel.Enqueue(
            new ProgressEntry(jobId, -1, null, "Final log before stop", DateTimeOffset.UtcNow)
        );

        // Give it a moment to pick up (WaitToReadAsync will return), then stop immediately
        await Task.Delay(200);
        await service.StopAsync(CancellationToken.None);

        // The drain-on-shutdown path should have flushed
        var updated = await _factory.Create().JobProgress.FindAsync(JobId.From(jobId));
        updated!.ProgressPercentage.Should().Be(90);
    }

    [Fact]
    public async Task Stop_WithEntriesFromMultipleJobs_DrainsAll()
    {
        var db = _factory.Create();
        var job1 = Guid.NewGuid();
        var job2 = Guid.NewGuid();
        var job3 = Guid.NewGuid();
        db.JobProgress.AddRange(CreateProgress(job1), CreateProgress(job2), CreateProgress(job3));
        await db.SaveChangesAsync();

        var channel = new ProgressChannel(NullLogger<ProgressChannel>.Instance);
        var service = CreateService(channel, flushIntervalMs: 30_000);
        await service.StartAsync(CancellationToken.None);

        channel.Enqueue(new ProgressEntry(job1, 10, "Job1", null, DateTimeOffset.UtcNow));
        channel.Enqueue(new ProgressEntry(job2, 20, "Job2", null, DateTimeOffset.UtcNow));
        channel.Enqueue(new ProgressEntry(job3, 30, "Job3", null, DateTimeOffset.UtcNow));

        await Task.Delay(200);
        await service.StopAsync(CancellationToken.None);

        var verifyDb = _factory.Create();
        (await verifyDb.JobProgress.FindAsync(JobId.From(job1)))!
            .ProgressPercentage.Should()
            .Be(10);
        (await verifyDb.JobProgress.FindAsync(JobId.From(job2)))!
            .ProgressPercentage.Should()
            .Be(20);
        (await verifyDb.JobProgress.FindAsync(JobId.From(job3)))!
            .ProgressPercentage.Should()
            .Be(30);
    }

    [Fact]
    public async Task Stop_ChannelEmpty_CompletesGracefully()
    {
        var channel = new ProgressChannel(NullLogger<ProgressChannel>.Instance);
        var service = CreateService(channel);
        await service.StartAsync(CancellationToken.None);
        await Task.Delay(200);

        var act = () => service.StopAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Stop_LogsAreDrainedToo()
    {
        var db = _factory.Create();
        var jobId = Guid.NewGuid();
        db.JobProgress.Add(CreateProgress(jobId));
        await db.SaveChangesAsync();

        var channel = new ProgressChannel(NullLogger<ProgressChannel>.Instance);
        var service = CreateService(channel, flushIntervalMs: 30_000);
        await service.StartAsync(CancellationToken.None);

        channel.Enqueue(
            new ProgressEntry(jobId, -1, null, "Log before shutdown", DateTimeOffset.UtcNow)
        );

        await Task.Delay(200);
        await service.StopAsync(CancellationToken.None);

        var updated = await _factory.Create().JobProgress.FindAsync(JobId.From(jobId));
        updated!.Logs.Should().NotBeNull();
        var logs = JsonSerializer.Deserialize<List<JobLogEntry>>(updated.Logs!);
        logs.Should().ContainSingle().Which.Message.Should().Be("Log before shutdown");
    }
}
