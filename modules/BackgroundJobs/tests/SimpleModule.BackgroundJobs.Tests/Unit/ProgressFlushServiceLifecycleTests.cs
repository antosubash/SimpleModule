using System.Text.Json;
using BackgroundJobs.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SimpleModule.BackgroundJobs.Contracts;
using SimpleModule.BackgroundJobs.Entities;
using SimpleModule.BackgroundJobs.Services;

namespace BackgroundJobs.Tests.Unit;

public sealed class ProgressFlushServiceLifecycleTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    // ===== START =====

    [Fact]
    public async Task Start_ProcessesEntriesAlreadyInChannel()
    {
        var db = _factory.Create();
        var jobId = Guid.NewGuid();
        db.JobProgress.Add(CreateProgress(jobId));
        await db.SaveChangesAsync();

        var channel = new ProgressChannel(NullLogger<ProgressChannel>.Instance);
        // Enqueue BEFORE starting the service
        channel.Enqueue(new ProgressEntry(jobId, 40, "Pre-queued", null, DateTimeOffset.UtcNow));

        var service = CreateService(channel);
        await service.StartAsync(CancellationToken.None);
        await Task.Delay(1500);
        await service.StopAsync(CancellationToken.None);

        var updated = await _factory.Create().JobProgress.FindAsync(jobId);
        updated!.ProgressPercentage.Should().Be(40);
        updated.ProgressMessage.Should().Be("Pre-queued");
    }

    [Fact]
    public async Task Start_ProcessesEntriesEnqueuedAfterStart()
    {
        var db = _factory.Create();
        var jobId = Guid.NewGuid();
        db.JobProgress.Add(CreateProgress(jobId));
        await db.SaveChangesAsync();

        var channel = new ProgressChannel(NullLogger<ProgressChannel>.Instance);
        var service = CreateService(channel);
        await service.StartAsync(CancellationToken.None);

        // Enqueue AFTER starting
        channel.Enqueue(new ProgressEntry(jobId, 60, "Post-start", null, DateTimeOffset.UtcNow));
        await Task.Delay(1500);
        await service.StopAsync(CancellationToken.None);

        var updated = await _factory.Create().JobProgress.FindAsync(jobId);
        updated!.ProgressPercentage.Should().Be(60);
        updated.ProgressMessage.Should().Be("Post-start");
    }

    [Fact]
    public async Task Start_EmptyChannel_DoesNotThrow()
    {
        var channel = new ProgressChannel(NullLogger<ProgressChannel>.Instance);
        var service = CreateService(channel);

        var act = async () =>
        {
            await service.StartAsync(CancellationToken.None);
            await Task.Delay(500);
            await service.StopAsync(CancellationToken.None);
        };

        await act.Should().NotThrowAsync();
    }

    // ===== STOP =====

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
        var updated = await _factory.Create().JobProgress.FindAsync(jobId);
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
        (await verifyDb.JobProgress.FindAsync(job1))!.ProgressPercentage.Should().Be(10);
        (await verifyDb.JobProgress.FindAsync(job2))!.ProgressPercentage.Should().Be(20);
        (await verifyDb.JobProgress.FindAsync(job3))!.ProgressPercentage.Should().Be(30);
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

        var updated = await _factory.Create().JobProgress.FindAsync(jobId);
        updated!.Logs.Should().NotBeNull();
        var logs = JsonSerializer.Deserialize<List<JobLogEntry>>(updated.Logs!);
        logs.Should().ContainSingle().Which.Message.Should().Be("Log before shutdown");
    }

    // ===== RESTART =====

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

        var afterFirst = await _factory.Create().JobProgress.FindAsync(jobId);
        afterFirst!.ProgressPercentage.Should().Be(25);

        // Restart with a new service instance (same channel)
        var service2 = CreateService(channel);
        await service2.StartAsync(CancellationToken.None);
        channel.Enqueue(new ProgressEntry(jobId, 80, "After restart", null, DateTimeOffset.UtcNow));
        await Task.Delay(1500);
        await service2.StopAsync(CancellationToken.None);

        var afterRestart = await _factory.Create().JobProgress.FindAsync(jobId);
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

        var updated = await _factory.Create().JobProgress.FindAsync(jobId);
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

        var updated = await _factory.Create().JobProgress.FindAsync(jobId);
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

        var updated = await _factory.Create().JobProgress.FindAsync(jobId);
        updated!.ProgressPercentage.Should().Be(90);
        updated.ProgressMessage.Should().Be("Cycle 3");

        var logs = JsonSerializer.Deserialize<List<JobLogEntry>>(updated.Logs!);
        logs.Should().HaveCount(3);
    }

    // ===== Helpers =====

    private static JobProgress CreateProgress(Guid id)
    {
        return new JobProgress
        {
            Id = id,
            JobTypeName = "TestJob",
            ModuleName = "Test",
            ProgressPercentage = 0,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
    }

    private ProgressFlushService CreateService(
        ProgressChannel channel,
        int flushIntervalMs = 500,
        int maxLogEntries = 1000
    )
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => _factory.Create());
        var scopeFactory = services
            .BuildServiceProvider()
            .GetRequiredService<IServiceScopeFactory>();

        var options = Options.Create(
            new BackgroundJobsModuleOptions
            {
                ProgressFlushBatchSize = 50,
                ProgressFlushInterval = TimeSpan.FromMilliseconds(flushIntervalMs),
                MaxLogEntries = maxLogEntries,
            }
        );

        return new ProgressFlushService(
            channel,
            scopeFactory,
            options,
            NullLogger<ProgressFlushService>.Instance
        );
    }
}
