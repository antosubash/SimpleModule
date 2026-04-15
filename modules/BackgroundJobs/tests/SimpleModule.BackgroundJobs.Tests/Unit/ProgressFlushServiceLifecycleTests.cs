using BackgroundJobs.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SimpleModule.BackgroundJobs.Contracts;
using SimpleModule.BackgroundJobs.Services;

namespace BackgroundJobs.Tests.Unit;

public sealed partial class ProgressFlushServiceLifecycleTests : IDisposable
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

        var updated = await _factory.Create().JobProgress.FindAsync(JobId.From(jobId));
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

        var updated = await _factory.Create().JobProgress.FindAsync(JobId.From(jobId));
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

    // ===== Helpers =====

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
