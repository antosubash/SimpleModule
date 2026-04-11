using System.Text.Json;
using BackgroundJobs.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SimpleModule.BackgroundJobs.Contracts;
using SimpleModule.BackgroundJobs.Entities;
using SimpleModule.BackgroundJobs.Services;

namespace BackgroundJobs.Tests.Unit;

public sealed class ProgressFlushServiceTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task FlushBatchAsync_UpdatesProgressPercentageAndMessage()
    {
        var db = _factory.Create();
        var jobId = Guid.NewGuid();
        db.JobProgress.Add(CreateProgress(jobId));
        await db.SaveChangesAsync();

        var channel = new ProgressChannel(NullLogger<ProgressChannel>.Instance);
        channel.Enqueue(new ProgressEntry(jobId, 75, "Processing", null, DateTimeOffset.UtcNow));

        var service = CreateService(channel, db);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        _ = service.StartAsync(cts.Token);

        // Wait for flush
        await Task.Delay(3000);
        await service.StopAsync(CancellationToken.None);

        var updated = await _factory.Create().JobProgress.FindAsync(JobId.From(jobId));
        updated!.ProgressPercentage.Should().Be(75);
        updated.ProgressMessage.Should().Be("Processing");
    }

    [Fact]
    public async Task FlushBatchAsync_AppendsLogEntries()
    {
        var db = _factory.Create();
        var jobId = Guid.NewGuid();
        db.JobProgress.Add(CreateProgress(jobId));
        await db.SaveChangesAsync();

        var channel = new ProgressChannel(NullLogger<ProgressChannel>.Instance);
        channel.Enqueue(new ProgressEntry(jobId, -1, null, "First log", DateTimeOffset.UtcNow));
        channel.Enqueue(new ProgressEntry(jobId, -1, null, "Second log", DateTimeOffset.UtcNow));

        var service = CreateService(channel, db);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        _ = service.StartAsync(cts.Token);
        await Task.Delay(3000);
        await service.StopAsync(CancellationToken.None);

        var updated = await _factory.Create().JobProgress.FindAsync(JobId.From(jobId));
        updated!.Logs.Should().NotBeNull();
        var logs = JsonSerializer.Deserialize<List<JobLogEntry>>(updated.Logs!);
        logs.Should().HaveCount(2);
        logs![0].Message.Should().Be("First log");
        logs[1].Message.Should().Be("Second log");
    }

    [Fact]
    public async Task FlushBatchAsync_CapsLogEntriesAtMaxLimit()
    {
        var db = _factory.Create();
        var jobId = Guid.NewGuid();

        // Pre-seed with 998 log entries
        var existingLogs = Enumerable
            .Range(0, 998)
            .Select(i => new JobLogEntry
            {
                Message = $"Entry {i}",
                Timestamp = DateTimeOffset.UtcNow,
            })
            .ToList();
        var progress = CreateProgress(jobId);
        progress.Logs = JsonSerializer.Serialize(existingLogs);
        db.JobProgress.Add(progress);
        await db.SaveChangesAsync();

        var channel = new ProgressChannel(NullLogger<ProgressChannel>.Instance);
        // Add 5 more — should cap at 1000
        for (var i = 0; i < 5; i++)
        {
            channel.Enqueue(
                new ProgressEntry(jobId, -1, null, $"New entry {i}", DateTimeOffset.UtcNow)
            );
        }

        var service = CreateService(channel, db, maxLogEntries: 1000);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        _ = service.StartAsync(cts.Token);
        await Task.Delay(3000);
        await service.StopAsync(CancellationToken.None);

        var updated = await _factory.Create().JobProgress.FindAsync(JobId.From(jobId));
        var logs = JsonSerializer.Deserialize<List<JobLogEntry>>(updated!.Logs!);
        logs.Should().HaveCount(1000);
        // Oldest entries should be dropped
        logs!.Last().Message.Should().Be("New entry 4");
    }

    [Fact]
    public async Task FlushBatchAsync_TakesLatestProgressPerJob()
    {
        var db = _factory.Create();
        var jobId = Guid.NewGuid();
        db.JobProgress.Add(CreateProgress(jobId));
        await db.SaveChangesAsync();

        var channel = new ProgressChannel(NullLogger<ProgressChannel>.Instance);
        channel.Enqueue(
            new ProgressEntry(
                jobId,
                25,
                "Quarter",
                null,
                DateTimeOffset.UtcNow.AddMilliseconds(-200)
            )
        );
        channel.Enqueue(
            new ProgressEntry(jobId, 50, "Half", null, DateTimeOffset.UtcNow.AddMilliseconds(-100))
        );
        channel.Enqueue(
            new ProgressEntry(jobId, 75, "Three quarters", null, DateTimeOffset.UtcNow)
        );

        var service = CreateService(channel, db);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        _ = service.StartAsync(cts.Token);
        await Task.Delay(3000);
        await service.StopAsync(CancellationToken.None);

        var updated = await _factory.Create().JobProgress.FindAsync(JobId.From(jobId));
        updated!.ProgressPercentage.Should().Be(75);
        updated.ProgressMessage.Should().Be("Three quarters");
    }

    [Fact]
    public async Task FlushBatchAsync_SkipsEntriesWithNoMatchingProgress()
    {
        var db = _factory.Create();
        var unknownJobId = Guid.NewGuid(); // no corresponding JobProgress row

        var channel = new ProgressChannel(NullLogger<ProgressChannel>.Instance);
        channel.Enqueue(
            new ProgressEntry(unknownJobId, 50, "Orphaned", null, DateTimeOffset.UtcNow)
        );

        var service = CreateService(channel, db);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        _ = service.StartAsync(cts.Token);
        await Task.Delay(3000);

        // Should not throw
        var act = () => service.StopAsync(CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task FlushBatchAsync_HandlesMultipleJobsInSameBatch()
    {
        var db = _factory.Create();
        var job1 = Guid.NewGuid();
        var job2 = Guid.NewGuid();
        db.JobProgress.AddRange(CreateProgress(job1), CreateProgress(job2));
        await db.SaveChangesAsync();

        var channel = new ProgressChannel(NullLogger<ProgressChannel>.Instance);
        channel.Enqueue(new ProgressEntry(job1, 30, "Job 1 progress", null, DateTimeOffset.UtcNow));
        channel.Enqueue(new ProgressEntry(job2, 60, "Job 2 progress", null, DateTimeOffset.UtcNow));

        var service = CreateService(channel, db);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        _ = service.StartAsync(cts.Token);
        await Task.Delay(3000);
        await service.StopAsync(CancellationToken.None);

        var verifyDb = _factory.Create();
        var p1 = await verifyDb.JobProgress.FindAsync(JobId.From(job1));
        var p2 = await verifyDb.JobProgress.FindAsync(JobId.From(job2));
        p1!.ProgressPercentage.Should().Be(30);
        p2!.ProgressPercentage.Should().Be(60);
    }

    // --- Helpers ---

    private static JobProgress CreateProgress(Guid id)
    {
        return new JobProgress
        {
            Id = JobId.From(id),
            JobTypeName = "TestJob",
            ModuleName = "Test",
            ProgressPercentage = 0,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
    }

    private ProgressFlushService CreateService(
        ProgressChannel channel,
        BackgroundJobsDbContext db,
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
                ProgressFlushInterval = TimeSpan.FromMilliseconds(500),
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
