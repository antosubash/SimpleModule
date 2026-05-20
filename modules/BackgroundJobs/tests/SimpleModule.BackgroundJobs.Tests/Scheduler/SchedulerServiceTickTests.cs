using BackgroundJobs.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using SimpleModule.BackgroundJobs.Contracts;
using SimpleModule.BackgroundJobs.Queue;
using SimpleModule.BackgroundJobs.Scheduler;
using SimpleModule.BackgroundJobs.Worker;

namespace SimpleModule.BackgroundJobs.Tests.Scheduler;

public sealed class SchedulerServiceTickTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();

    [Fact]
    public async Task Tick_EnqueuesDueJob()
    {
        await using var harness = CreateHarness(out var registry, "host-A");
        registry.Job<FakeJobA>("a").EveryMinutes(1);

        // First tick at T=0 — reconcile + maybe enqueue, depending on cron.
        await harness.Service.TickOnceAsync(CancellationToken.None);

        // Advance the clock 90 seconds so NextRunAt is definitely in the past.
        harness.Clock.Advance(TimeSpan.FromSeconds(90));
        await harness.Service.TickOnceAsync(CancellationToken.None);

        await using var verify = _factory.Create();
        var entries = await verify
            .JobQueueEntries.Where(e =>
                e.RecurringName == SchedulerOptions.ScheduledJobSentinel + "a"
            )
            .ToListAsync();
        entries.Should().NotBeEmpty();
        entries[0].JobTypeName.Should().Be(typeof(FakeJobA).AssemblyQualifiedName);
    }

    [Fact]
    public async Task Tick_SkipsWhenNextRunInFuture()
    {
        await using var harness = CreateHarness(out var registry, "host-A");
        registry.Job<FakeJobA>("a").Daily(); // next run hours away

        await harness.Service.TickOnceAsync(CancellationToken.None);
        // Don't advance the clock — re-tick immediately.
        await harness.Service.TickOnceAsync(CancellationToken.None);

        await using var verify = _factory.Create();
        var entries = await verify
            .JobQueueEntries.Where(e =>
                e.RecurringName == SchedulerOptions.ScheduledJobSentinel + "a"
            )
            .ToListAsync();
        entries.Should().BeEmpty();
    }

    [Fact]
    public async Task Tick_ContinuesPastBadCronDefinition()
    {
        await using var harness = CreateHarness(out var registry, "host-A");
        registry.Job<FakeJobA>("bad").Cron("this is not a cron");
        registry.Job<FakeJobB>("good").EveryMinutes(1);

        await harness.Service.TickOnceAsync(CancellationToken.None);
        harness.Clock.Advance(TimeSpan.FromSeconds(90));
        await harness.Service.TickOnceAsync(CancellationToken.None);

        await using var verify = _factory.Create();
        var goodEntries = await verify
            .JobQueueEntries.Where(e =>
                e.RecurringName == SchedulerOptions.ScheduledJobSentinel + "good"
            )
            .ToListAsync();
        goodEntries.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Tick_OnOneServer_OnlyOneHostEnqueues()
    {
        await using var harnessA = CreateHarness(out var registryA, "host-A");
        await using var harnessB = CreateHarness(out var registryB, "host-B");

        registryA.Job<FakeJobA>("shared").EveryMinutes(1).OnOneServer();
        registryB.Job<FakeJobA>("shared").EveryMinutes(1).OnOneServer();

        await harnessA.Service.TickOnceAsync(CancellationToken.None);
        await harnessB.Service.TickOnceAsync(CancellationToken.None);
        harnessA.Clock.Advance(TimeSpan.FromSeconds(90));
        harnessB.Clock.Advance(TimeSpan.FromSeconds(90));

        await harnessA.Service.TickOnceAsync(CancellationToken.None);
        await harnessB.Service.TickOnceAsync(CancellationToken.None);

        await using var verify = _factory.Create();
        var entries = await verify
            .JobQueueEntries.Where(e =>
                e.RecurringName == SchedulerOptions.ScheduledJobSentinel + "shared"
            )
            .ToListAsync();
        entries.Should().HaveCount(1);
    }

    [Fact]
    public async Task Tick_WithoutOverlapping_SkipsWhenMutexHeld()
    {
        await using var harness = CreateHarness(out var registry, "host-A");
        registry.Job<FakeJobA>("locked").EveryMinutes(1).WithoutOverlapping();

        // Pre-acquire the mutex outside the scheduler so the tick can't take it.
        await using (var pre = _factory.Create())
        {
            var mutex = new DatabaseJobMutex(pre, NullLogger<DatabaseJobMutex>.Instance);
            (
                await mutex.TryAcquireAsync(
                    SchedulerService.MutexNameFor("locked"),
                    "external-owner",
                    TimeSpan.FromMinutes(5)
                )
            ).Should().BeTrue();
        }

        await harness.Service.TickOnceAsync(CancellationToken.None);
        harness.Clock.Advance(TimeSpan.FromSeconds(90));
        await harness.Service.TickOnceAsync(CancellationToken.None);

        await using var verify = _factory.Create();
        var entries = await verify
            .JobQueueEntries.Where(e =>
                e.RecurringName == SchedulerOptions.ScheduledJobSentinel + "locked"
            )
            .ToListAsync();
        entries.Should().BeEmpty();
    }

    private SchedulerHarness CreateHarness(out SchedulerRegistry registry, string ownerId)
    {
        var clock = new FakeTimeProvider(
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
        );
        var reg = new SchedulerRegistry();
        registry = reg;

        var sp = new ServiceCollection()
            .AddScoped(_ => _factory.Create())
            .AddScoped<IJobQueue, DatabaseJobQueue>()
            .AddScoped<IJobMutex, DatabaseJobMutex>()
            .AddScoped<IInstanceLeader, DatabaseInstanceLeader>()
            .AddLogging()
            .BuildServiceProvider();

        var service = new SchedulerService(
            sp.GetRequiredService<IServiceScopeFactory>(),
            reg,
            new WorkerIdentity(ownerId),
            Options.Create(new SchedulerOptions { TickInterval = TimeSpan.FromSeconds(30) }),
            clock,
            NullLogger<SchedulerService>.Instance
        );

        return new SchedulerHarness(sp, service, clock);
    }

    public void Dispose()
    {
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }

    private sealed class SchedulerHarness(
        ServiceProvider sp,
        SchedulerService service,
        FakeTimeProvider clock
    ) : IAsyncDisposable
    {
        public SchedulerService Service { get; } = service;
        public FakeTimeProvider Clock { get; } = clock;

        public async ValueTask DisposeAsync()
        {
            Service.Dispose();
            await sp.DisposeAsync();
        }
    }
}
