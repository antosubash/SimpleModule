using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SimpleModule.BackgroundJobs.Contracts;
using SimpleModule.BackgroundJobs.Services;
using TickerQ.Utilities.Models;

namespace BackgroundJobs.Tests.Unit;

public sealed class JobExecutionBridgeTests
{
    private readonly JobTypeRegistry _registry = new();
    private readonly ProgressChannel _channel = new();

    [Fact]
    public async Task ExecuteAsync_WithRegisteredJob_ExecutesJob()
    {
        var executed = false;
        var testJob = new TestJob(() => executed = true);
        _registry.Register(typeof(TestJob));

        var services = CreateServiceProvider(testJob);
        var bridge = new JobExecutionBridge(
            services,
            _registry,
            _channel,
            NullLogger<JobExecutionBridge>.Instance
        );

        var context = CreateContext<TestJob>(null);

        await bridge.ExecuteAsync(context, CancellationToken.None);

        executed.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_UnregisteredJobType_ThrowsInvalidOperation()
    {
        var services = CreateServiceProvider(new TestJob(() => { }));
        var bridge = new JobExecutionBridge(
            services,
            _registry, // empty registry
            _channel,
            NullLogger<JobExecutionBridge>.Instance
        );

        var context = CreateContext<TestJob>(null);

        var act = () => bridge.ExecuteAsync(context, CancellationToken.None);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*Unregistered job type*");
    }

    [Fact]
    public async Task ExecuteAsync_ReportsProgressZeroAndHundred()
    {
        _registry.Register(typeof(TestJob));
        var services = CreateServiceProvider(new TestJob(() => { }));
        var bridge = new JobExecutionBridge(
            services,
            _registry,
            _channel,
            NullLogger<JobExecutionBridge>.Instance
        );

        var context = CreateContext<TestJob>(null);
        await bridge.ExecuteAsync(context, CancellationToken.None);

        // Should have at least 2 entries: 0% start and 100% complete
        var entries = new List<ProgressEntry>();
        while (_channel.Reader.TryRead(out var entry))
        {
            entries.Add(entry);
        }
        entries.Should().HaveCountGreaterOrEqualTo(2);
        entries.First().Percentage.Should().Be(0);
        entries.First().Message.Should().Be("Starting");
        entries.Last().Percentage.Should().Be(100);
        entries.Last().Message.Should().Be("Completed");
    }

    [Fact]
    public async Task ExecuteAsync_PassesCancellationToken()
    {
        CancellationToken receivedToken = default;
        var testJob = new TestJobWithToken(ct => receivedToken = ct);
        _registry.Register(typeof(TestJobWithToken));

        var services = CreateServiceProvider(testJob);
        var bridge = new JobExecutionBridge(
            services,
            _registry,
            _channel,
            NullLogger<JobExecutionBridge>.Instance
        );

        using var cts = new CancellationTokenSource();
        var context = CreateContext<TestJobWithToken>(null);
        await bridge.ExecuteAsync(context, cts.Token);

        receivedToken.Should().Be(cts.Token);
    }

    [Fact]
    public async Task ExecuteAsync_JobThrows_ExceptionPropagates()
    {
        var testJob = new ThrowingJob();
        _registry.Register(typeof(ThrowingJob));

        var services = CreateServiceProvider(testJob);
        var bridge = new JobExecutionBridge(
            services,
            _registry,
            _channel,
            NullLogger<JobExecutionBridge>.Instance
        );

        var context = CreateContext<ThrowingJob>(null);

        var act = () => bridge.ExecuteAsync(context, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Job failed!");
    }

    [Fact]
    public async Task ExecuteAsync_WithData_ContextHasAccessToData()
    {
        var data = new TestData("import", 100);
        string? receivedName = null;
        var testJob = new DataReadingJob(ctx =>
        {
            var d = ctx.GetData<TestData>();
            receivedName = d.Name;
        });
        _registry.Register(typeof(DataReadingJob));

        var services = CreateServiceProvider(testJob);
        var bridge = new JobExecutionBridge(
            services,
            _registry,
            _channel,
            NullLogger<JobExecutionBridge>.Instance
        );

        var context = CreateContext<DataReadingJob>(data);
        await bridge.ExecuteAsync(context, CancellationToken.None);

        receivedName.Should().Be("import");
    }

    // --- Helpers ---

    private static TickerFunctionContext<JobDispatchPayload> CreateContext<TJob>(object? data)
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

    private static IServiceProvider CreateServiceProvider(IModuleJob job)
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => job);
        services.AddScoped(job.GetType(), _ => job);
        return services.BuildServiceProvider();
    }

    private sealed class TestJob(Action onExecute) : IModuleJob
    {
        public Task ExecuteAsync(IJobExecutionContext context, CancellationToken ct)
        {
            onExecute();
            return Task.CompletedTask;
        }
    }

    private sealed class TestJobWithToken(Action<CancellationToken> onExecute) : IModuleJob
    {
        public Task ExecuteAsync(IJobExecutionContext context, CancellationToken ct)
        {
            onExecute(ct);
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingJob : IModuleJob
    {
        public Task ExecuteAsync(IJobExecutionContext context, CancellationToken ct)
        {
            throw new InvalidOperationException("Job failed!");
        }
    }

    private sealed class DataReadingJob(Action<IJobExecutionContext> onExecute) : IModuleJob
    {
        public Task ExecuteAsync(IJobExecutionContext context, CancellationToken ct)
        {
            onExecute(context);
            return Task.CompletedTask;
        }
    }

    private record TestData(string Name, int Count);
}
