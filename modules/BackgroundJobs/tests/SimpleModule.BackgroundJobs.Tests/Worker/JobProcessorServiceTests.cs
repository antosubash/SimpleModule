using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SimpleModule.BackgroundJobs.Contracts;
using SimpleModule.BackgroundJobs.Queue;
using SimpleModule.BackgroundJobs.Services;
using SimpleModule.BackgroundJobs.Worker;
using SimpleModule.Database;

namespace SimpleModule.BackgroundJobs.Tests.Worker;

public class JobProcessorServiceTests
{
    // Test job that signals completion via a shared TCS
    public sealed class SignalJob(SignalJob.Signal signal) : IModuleJob
    {
        public sealed class Signal { public TaskCompletionSource<Guid> Tcs { get; } = new(); }

        public Task ExecuteAsync(IJobExecutionContext context, CancellationToken cancellationToken)
        {
            signal.Tcs.TrySetResult(context.JobId.Value);
            return Task.CompletedTask;
        }
    }

    private static ServiceProvider BuildProvider(string dbName, SignalJob.Signal signal)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(signal);
        services.AddDbContext<BackgroundJobsDbContext>(o =>
            o.UseSqlite($"DataSource=file:{dbName}?mode=memory&cache=shared"));
        services.AddSingleton(Options.Create(new DatabaseOptions
        {
            DefaultConnection = $"Data Source=file:{dbName}?mode=memory&cache=shared",
        }));
        services.AddSingleton(Options.Create(new BackgroundJobsWorkerOptions
        {
            MaxConcurrency = 2,
            PollInterval = TimeSpan.FromMilliseconds(50),
            StallTimeout = TimeSpan.FromMinutes(5),
            StallSweepInterval = TimeSpan.FromMinutes(10),
            MaxAttempts = 1,
            RetryBaseDelay = TimeSpan.FromSeconds(1),
        }));
        services.AddScoped<SignalJob>();
        services.AddSingleton(sp =>
        {
            var r = new JobTypeRegistry();
            r.Register(typeof(SignalJob));
            return r;
        });
        services.AddSingleton<ProgressChannel>();
        services.AddScoped<IJobQueue, DatabaseJobQueue>();
        services.AddSingleton(WorkerIdentity.Create());
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task EnqueuedJobIsExecutedByProcessor()
    {
        var signal = new SignalJob.Signal();
        var dbName = Guid.NewGuid().ToString();
        using var sp = BuildProvider(dbName, signal);

        // Open a keep-alive connection so shared-cache SQLite stays alive
        using var keepalive = sp.GetRequiredService<BackgroundJobsDbContext>();
        await keepalive.Database.OpenConnectionAsync();
        await keepalive.Database.EnsureCreatedAsync();

        // Enqueue directly via IJobQueue
        await using (var scope = sp.CreateAsyncScope())
        {
            var queue = scope.ServiceProvider.GetRequiredService<IJobQueue>();
            await queue.EnqueueAsync(new JobQueueEntry(
                Guid.NewGuid(),
                typeof(SignalJob).AssemblyQualifiedName!,
                null,
                DateTimeOffset.UtcNow,
                JobQueueEntryState.Pending,
                0, null, null, DateTimeOffset.UtcNow));
        }

        var processor = ActivatorUtilities.CreateInstance<JobProcessorService>(sp);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var run = processor.StartAsync(cts.Token);

        await Task.WhenAny(signal.Tcs.Task, Task.Delay(5000, cts.Token));
        signal.Tcs.Task.IsCompletedSuccessfully.Should().BeTrue("the job should have executed");

        // Give the processor a moment to call CompleteAsync after the job finishes.
        await Task.Delay(200, CancellationToken.None);
        await processor.StopAsync(CancellationToken.None);

        // Confirm queue row is Completed
        using var verifyDb = sp.GetRequiredService<BackgroundJobsDbContext>();
        var row = await verifyDb.JobQueueEntries.AsNoTracking().SingleAsync();
        row.State.Should().Be(JobQueueEntryState.Completed);
    }

    [Fact]
    public async Task TwoProcessorsDoNotDoubleExecute()
    {
        var signal = new SignalJob.Signal();
        var dbName = Guid.NewGuid().ToString();
        using var sp = BuildProvider(dbName, signal);

        using var keepalive = sp.GetRequiredService<BackgroundJobsDbContext>();
        await keepalive.Database.OpenConnectionAsync();
        await keepalive.Database.EnsureCreatedAsync();

        // Enqueue 10 jobs
        await using (var scope = sp.CreateAsyncScope())
        {
            var queue = scope.ServiceProvider.GetRequiredService<IJobQueue>();
            for (int i = 0; i < 10; i++)
            {
                await queue.EnqueueAsync(new JobQueueEntry(
                    Guid.NewGuid(),
                    typeof(SignalJob).AssemblyQualifiedName!,
                    null,
                    DateTimeOffset.UtcNow,
                    JobQueueEntryState.Pending,
                    0, null, null, DateTimeOffset.UtcNow));
            }
        }

        var p1 = ActivatorUtilities.CreateInstance<JobProcessorService>(sp);
        var p2 = ActivatorUtilities.CreateInstance<JobProcessorService>(sp);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        await p1.StartAsync(cts.Token);
        await p2.StartAsync(cts.Token);

        // Wait until all 10 are completed or timeout
        using var db = sp.GetRequiredService<BackgroundJobsDbContext>();
        while (!cts.IsCancellationRequested)
        {
            var completed = await db.JobQueueEntries.CountAsync(e => e.State == JobQueueEntryState.Completed);
            if (completed == 10) break;
            await Task.Delay(100, cts.Token);
        }

        await p1.StopAsync(CancellationToken.None);
        await p2.StopAsync(CancellationToken.None);

        var all = await db.JobQueueEntries.AsNoTracking().ToListAsync();
        all.Should().HaveCount(10);
        all.Should().OnlyContain(r => r.State == JobQueueEntryState.Completed);
        all.Should().OnlyContain(r => r.AttemptCount == 1, "each job should have been claimed exactly once");
    }
}
