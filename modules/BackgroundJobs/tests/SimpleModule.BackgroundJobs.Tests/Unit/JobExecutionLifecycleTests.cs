using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SimpleModule.BackgroundJobs.Contracts;
using SimpleModule.BackgroundJobs.Services;
using TickerQ.Utilities.Base;

namespace BackgroundJobs.Tests.Unit;

public sealed class JobExecutionLifecycleTests
{
    private readonly JobTypeRegistry _registry = new();
    private readonly ProgressChannel _channel = new();

    // ===== STOP (Cancellation) =====

    [Fact]
    public async Task Stop_CancellationTokenCancelled_JobReceivesCancellation()
    {
        var wasCancelled = false;
        var job = new LongRunningJob(async ct =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(30), ct);
            }
            catch (OperationCanceledException)
            {
                wasCancelled = true;
                throw;
            }
        });
        _registry.Register(typeof(LongRunningJob));

        var bridge = CreateBridge(job);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        var context = CreateContext<LongRunningJob>();

        var act = () => bridge.ExecuteAsync(context, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        wasCancelled.Should().BeTrue();
    }

    [Fact]
    public async Task Stop_JobChecksToken_StopsGracefully()
    {
        var processedItems = 0;
        var job = new IterativeJob(async ct =>
        {
            for (var i = 0; i < 1000; i++)
            {
                ct.ThrowIfCancellationRequested();
                processedItems++;
                await Task.Delay(10, ct);
            }
        });
        _registry.Register(typeof(IterativeJob));

        var bridge = CreateBridge(job);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        var context = CreateContext<IterativeJob>();

        var act = () => bridge.ExecuteAsync(context, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        processedItems.Should().BeGreaterThan(0).And.BeLessThan(1000);
    }

    [Fact]
    public async Task Stop_CancelledJob_DoesNotReportCompleted()
    {
        var job = new LongRunningJob(async ct =>
        {
            await Task.Delay(TimeSpan.FromSeconds(30), ct);
        });
        _registry.Register(typeof(LongRunningJob));

        var bridge = CreateBridge(job);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        var context = CreateContext<LongRunningJob>();

        try
        {
            await bridge.ExecuteAsync(context, cts.Token);
        }
        catch (OperationCanceledException)
        {
            // expected
        }

        // Read all progress entries
        var entries = new List<ProgressEntry>();
        while (_channel.Reader.TryRead(out var entry))
        {
            entries.Add(entry);
        }

        // Should have "Starting" (0%) but NOT "Completed" (100%)
        entries.Should().Contain(e => e.Percentage == 0 && e.Message == "Starting");
        entries.Should().NotContain(e => e.Percentage == 100 && e.Message == "Completed");
    }

    // ===== START =====

    [Fact]
    public async Task Start_JobReportsIntermediateProgress()
    {
        var job = new ProgressReportingJob(async (ctx, ct) =>
        {
            ctx.ReportProgress(25, "Step 1");
            ctx.ReportProgress(50, "Step 2");
            ctx.ReportProgress(75, "Step 3");
            await Task.CompletedTask;
        });
        _registry.Register(typeof(ProgressReportingJob));

        var bridge = CreateBridge(job);
        var context = CreateContext<ProgressReportingJob>();

        await bridge.ExecuteAsync(context, CancellationToken.None);

        var entries = DrainChannel();
        // 0% (auto) + 25% + 50% + 75% + 100% (auto) = 5 entries
        entries.Should().HaveCount(5);
        entries[0].Percentage.Should().Be(0);
        entries[1].Percentage.Should().Be(25);
        entries[2].Percentage.Should().Be(50);
        entries[3].Percentage.Should().Be(75);
        entries[4].Percentage.Should().Be(100);
    }

    [Fact]
    public async Task Start_JobLogs_AllLogsEnqueued()
    {
        var job = new ProgressReportingJob(async (ctx, ct) =>
        {
            ctx.Log("Starting import");
            ctx.Log("Row 1 processed");
            ctx.Log("Row 2 processed");
            await Task.CompletedTask;
        });
        _registry.Register(typeof(ProgressReportingJob));

        var bridge = CreateBridge(job);
        var context = CreateContext<ProgressReportingJob>();

        await bridge.ExecuteAsync(context, CancellationToken.None);

        var entries = DrainChannel();
        var logEntries = entries.Where(e => e.LogMessage is not null).ToList();
        logEntries.Should().HaveCount(3);
        logEntries[0].LogMessage.Should().Be("Starting import");
        logEntries[1].LogMessage.Should().Be("Row 1 processed");
        logEntries[2].LogMessage.Should().Be("Row 2 processed");
    }

    [Fact]
    public async Task Start_JobWithDataPayload_CanAccessData()
    {
        string? receivedValue = null;
        var job = new ProgressReportingJob(async (ctx, ct) =>
        {
            var data = ctx.GetData<ImportConfig>();
            receivedValue = data.FilePath;
            await Task.CompletedTask;
        });
        _registry.Register(typeof(ProgressReportingJob));

        var bridge = CreateBridge(job);
        var context = CreateContext<ProgressReportingJob>(new ImportConfig("/data/file.csv", 100));

        await bridge.ExecuteAsync(context, CancellationToken.None);

        receivedValue.Should().Be("/data/file.csv");
    }

    // ===== RESTART (after failure) =====

    [Fact]
    public async Task Restart_AfterFailure_NewExecutionStartsFresh()
    {
        var callCount = 0;
        var job = new ProgressReportingJob(async (ctx, ct) =>
        {
            callCount++;
            if (callCount == 1)
            {
                ctx.ReportProgress(30, "Will fail");
                throw new InvalidOperationException("Transient error");
            }
            ctx.ReportProgress(50, "Retry succeeding");
            await Task.CompletedTask;
        });
        _registry.Register(typeof(ProgressReportingJob));

        var bridge = CreateBridge(job);

        // First execution - fails
        var context1 = CreateContext<ProgressReportingJob>();
        var act1 = () => bridge.ExecuteAsync(context1, CancellationToken.None);
        await act1.Should().ThrowAsync<InvalidOperationException>();

        // Drain progress from failed run
        var failedEntries = DrainChannel();
        failedEntries.Should().Contain(e => e.Message == "Will fail");
        failedEntries.Should().NotContain(e => e.Message == "Completed");

        // Second execution (retry) - succeeds
        var context2 = CreateContext<ProgressReportingJob>();
        await bridge.ExecuteAsync(context2, CancellationToken.None);

        var successEntries = DrainChannel();
        successEntries.Should().Contain(e => e.Message == "Starting");
        successEntries.Should().Contain(e => e.Message == "Retry succeeding");
        successEntries.Should().Contain(e => e.Percentage == 100 && e.Message == "Completed");
    }

    [Fact]
    public async Task Restart_AfterCancellation_NewExecutionStartsFresh()
    {
        var runNumber = 0;
        var job = new LongRunningJob(async ct =>
        {
            runNumber++;
            if (runNumber == 1)
            {
                await Task.Delay(TimeSpan.FromSeconds(30), ct);
            }
            // Second run completes immediately
        });
        _registry.Register(typeof(LongRunningJob));

        var bridge = CreateBridge(job);

        // First execution - cancelled
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        var context1 = CreateContext<LongRunningJob>();
        try
        {
            await bridge.ExecuteAsync(context1, cts.Token);
        }
        catch (OperationCanceledException)
        {
            // expected
        }

        DrainChannel(); // clear

        // Second execution - completes
        var context2 = CreateContext<LongRunningJob>();
        await bridge.ExecuteAsync(context2, CancellationToken.None);

        var entries = DrainChannel();
        entries.Should().Contain(e => e.Percentage == 0 && e.Message == "Starting");
        entries.Should().Contain(e => e.Percentage == 100 && e.Message == "Completed");
    }

    [Fact]
    public async Task Restart_ProgressChannelReceivesEntriesFromBothRuns()
    {
        var job = new ProgressReportingJob(async (ctx, ct) =>
        {
            ctx.ReportProgress(50, "Working");
            await Task.CompletedTask;
        });
        _registry.Register(typeof(ProgressReportingJob));

        var bridge = CreateBridge(job);

        // Run 1
        var context1 = CreateContext<ProgressReportingJob>();
        await bridge.ExecuteAsync(context1, CancellationToken.None);
        var run1Entries = DrainChannel();

        // Run 2
        var context2 = CreateContext<ProgressReportingJob>();
        await bridge.ExecuteAsync(context2, CancellationToken.None);
        var run2Entries = DrainChannel();

        // Both runs should produce identical progress patterns
        run1Entries.Should().HaveCount(run2Entries.Count);
        run1Entries.Select(e => e.Percentage)
            .Should()
            .BeEquivalentTo(run2Entries.Select(e => e.Percentage));
    }

    // ===== Helpers =====

    private JobExecutionBridge CreateBridge(IModuleJob job)
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => job);
        services.AddScoped(job.GetType(), _ => job);
        var provider = services.BuildServiceProvider();

        return new JobExecutionBridge(
            provider,
            _registry,
            _channel,
            NullLogger<JobExecutionBridge>.Instance
        );
    }

    private static TickerFunctionContext<JobDispatchPayload> CreateContext<TJob>(object? data = null)
        where TJob : IModuleJob
    {
        var payload = new JobDispatchPayload(
            typeof(TJob).AssemblyQualifiedName!,
            data is not null ? JsonSerializer.Serialize(data) : null
        );
        var context = Substitute.For<TickerFunctionContext<JobDispatchPayload>>();
        context.Id.Returns(Guid.NewGuid());
        context.Request.Returns(payload);
        return context;
    }

    private List<ProgressEntry> DrainChannel()
    {
        var entries = new List<ProgressEntry>();
        while (_channel.Reader.TryRead(out var entry))
        {
            entries.Add(entry);
        }
        return entries;
    }

    // --- Test Job Implementations ---

    private sealed class LongRunningJob(Func<CancellationToken, Task> action) : IModuleJob
    {
        public Task ExecuteAsync(IJobExecutionContext context, CancellationToken ct) => action(ct);
    }

    private sealed class IterativeJob(Func<CancellationToken, Task> action) : IModuleJob
    {
        public Task ExecuteAsync(IJobExecutionContext context, CancellationToken ct) => action(ct);
    }

    private sealed class ProgressReportingJob(
        Func<IJobExecutionContext, CancellationToken, Task> action
    ) : IModuleJob
    {
        public Task ExecuteAsync(IJobExecutionContext context, CancellationToken ct) =>
            action(context, ct);
    }

    private record ImportConfig(string FilePath, int BatchSize);
}
