using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SimpleModule.BackgroundJobs.Contracts;
using SimpleModule.BackgroundJobs.Services;

namespace BackgroundJobs.Tests.Unit;

public sealed partial class ProgressFlushServiceLifecycleTests
{
    [Fact]
    public async Task Restart_ProcessesNewEntriesAfterRestart()
    {
        var db = _factory.Create();
        var jobId = Guid.NewGuid();
        db.JobProgress.Add(CreateProgress(jobId));
        await db.SaveChangesAsync();

        var channel = new ProgressChannel(NullLogger<ProgressChannel>.Instance);
        var service = CreateService(channel);

        // First run
        await service.StartAsync(CancellationToken.None);
        channel.Enqueue(new ProgressEntry(jobId, 25, "First run", null, DateTimeOffset.UtcNow));
        await Task.Delay(1500);
        await service.StopAsync(CancellationToken.None);

        var afterFirst = await _factory.Create().JobProgress.FindAsync(JobId.From(jobId));
        afterFirst!.ProgressPercentage.Should().Be(25);

        // Restart with a new service instance (same channel)
        var service2 = CreateService(channel);
        await service2.StartAsync(CancellationToken.None);
        channel.Enqueue(new ProgressEntry(jobId, 80, "After restart", null, DateTimeOffset.UtcNow));
        await Task.Delay(1500);
        await service2.StopAsync(CancellationToken.None);

        var afterRestart = await _factory.Create().JobProgress.FindAsync(JobId.From(jobId));
        afterRestart!.ProgressPercentage.Should().Be(80);
        afterRestart.ProgressMessage.Should().Be("After restart");
    }

    [Fact]
    public async Task Restart_EntriesEnqueuedBetweenStopAndStart_AreProcessed()
    {
        var db = _factory.Create();
        var jobId = Guid.NewGuid();
        db.JobProgress.Add(CreateProgress(jobId));
        await db.SaveChangesAsync();

        var channel = new ProgressChannel(NullLogger<ProgressChannel>.Instance);
        var service = CreateService(channel);

        // First run and stop
        await service.StartAsync(CancellationToken.None);
        await Task.Delay(200);
        await service.StopAsync(CancellationToken.None);

        // Enqueue while service is stopped
        channel.Enqueue(
            new ProgressEntry(jobId, 50, "Enqueued while stopped", null, DateTimeOffset.UtcNow)
        );

        // Restart
        var service2 = CreateService(channel);
        await service2.StartAsync(CancellationToken.None);
        await Task.Delay(1500);
        await service2.StopAsync(CancellationToken.None);

        var updated = await _factory.Create().JobProgress.FindAsync(JobId.From(jobId));
        updated!.ProgressPercentage.Should().Be(50);
        updated.ProgressMessage.Should().Be("Enqueued while stopped");
    }

    [Fact]
    public async Task Restart_LogsContinueAppending()
    {
        var db = _factory.Create();
        var jobId = Guid.NewGuid();
        db.JobProgress.Add(CreateProgress(jobId));
        await db.SaveChangesAsync();

        var channel = new ProgressChannel(NullLogger<ProgressChannel>.Instance);

        // First run - add log
        var service1 = CreateService(channel);
        await service1.StartAsync(CancellationToken.None);
        channel.Enqueue(
            new ProgressEntry(jobId, -1, null, "Log from run 1", DateTimeOffset.UtcNow)
        );
        await Task.Delay(1500);
        await service1.StopAsync(CancellationToken.None);

        // Second run - add another log
        var service2 = CreateService(channel);
        await service2.StartAsync(CancellationToken.None);
        channel.Enqueue(
            new ProgressEntry(jobId, -1, null, "Log from run 2", DateTimeOffset.UtcNow)
        );
        await Task.Delay(1500);
        await service2.StopAsync(CancellationToken.None);

        var updated = await _factory.Create().JobProgress.FindAsync(JobId.From(jobId));
        var logs = JsonSerializer.Deserialize<List<JobLogEntry>>(updated!.Logs!);
        logs.Should().HaveCount(2);
        logs![0].Message.Should().Be("Log from run 1");
        logs[1].Message.Should().Be("Log from run 2");
    }

    [Fact]
    public async Task Restart_MultipleStopStartCycles_NoDataLoss()
    {
        var db = _factory.Create();
        var jobId = Guid.NewGuid();
        db.JobProgress.Add(CreateProgress(jobId));
        await db.SaveChangesAsync();

        var channel = new ProgressChannel(NullLogger<ProgressChannel>.Instance);

        for (var cycle = 1; cycle <= 3; cycle++)
        {
            var service = CreateService(channel);
            await service.StartAsync(CancellationToken.None);
            channel.Enqueue(
                new ProgressEntry(
                    jobId,
                    cycle * 30,
                    $"Cycle {cycle}",
                    $"Log cycle {cycle}",
                    DateTimeOffset.UtcNow
                )
            );
            await Task.Delay(1500);
            await service.StopAsync(CancellationToken.None);
        }

        var updated = await _factory.Create().JobProgress.FindAsync(JobId.From(jobId));
        updated!.ProgressPercentage.Should().Be(90);
        updated.ProgressMessage.Should().Be("Cycle 3");

        var logs = JsonSerializer.Deserialize<List<JobLogEntry>>(updated.Logs!);
        logs.Should().HaveCount(3);
    }
}
