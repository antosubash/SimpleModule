using System.Text.Json;
using BackgroundJobs.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SimpleModule.BackgroundJobs.Contracts;
using SimpleModule.BackgroundJobs.Services;
using TickerQ.Utilities.Entities;
using TickerQ.Utilities.Enums;
using TickerQ.Utilities.Interfaces.Managers;

namespace BackgroundJobs.Tests.Unit;

public sealed class BackgroundJobsServiceTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly BackgroundJobsDbContext _db;
    private readonly ITimeTickerManager<TimeTickerEntity> _timeManager =
        Substitute.For<ITimeTickerManager<TimeTickerEntity>>();
    private readonly ICronTickerManager<CronTickerEntity> _cronManager =
        Substitute.For<ICronTickerManager<CronTickerEntity>>();
    private readonly BackgroundJobsService _sut;

    public BackgroundJobsServiceTests()
    {
        _db = _factory.Create();
        _sut = new BackgroundJobsService(
            _timeManager,
            _cronManager,
            _db,
            NullLogger<BackgroundJobsService>.Instance
        );
    }

    public void Dispose()
    {
        _db.Dispose();
        _factory.Dispose();
    }

    // --- EnqueueAsync ---

    [Fact]
    public async Task EnqueueAsync_CreatesTimeTickerAndProgress()
    {
        var jobId = await _sut.EnqueueAsync<TestJob>(new TestData("import", 100), CancellationToken.None);

        jobId.Value.Should().NotBeEmpty();
        await _timeManager.Received(1).AddAsync(Arg.Any<TimeTickerEntity>());

        var progress = await _db.JobProgress.FindAsync(jobId.Value);
        progress.Should().NotBeNull();
        progress!.ProgressPercentage.Should().Be(0);
        progress.JobTypeName.Should().Contain("TestJob");
    }

    [Fact]
    public async Task EnqueueAsync_SerializesPayloadWithJobTypeName()
    {
        TimeTickerEntity? capturedTicker = null;
        _ = _timeManager.AddAsync(Arg.Do<TimeTickerEntity>(t => capturedTicker = t));

        await _sut.EnqueueAsync<TestJob>(new TestData("test", 1), CancellationToken.None);

        capturedTicker.Should().NotBeNull();
        capturedTicker!.Function.Should().Be("ModuleJobDispatcher");

        var payload = JsonSerializer.Deserialize<JobDispatchPayload>(capturedTicker.Request);
        payload.Should().NotBeNull();
        payload!.JobTypeName.Should().Contain("TestJob");
        payload.SerializedData.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task EnqueueAsync_WithNullData_SerializesNullPayloadData()
    {
        TimeTickerEntity? capturedTicker = null;
        _ = _timeManager.AddAsync(Arg.Do<TimeTickerEntity>(t => capturedTicker = t));

        await _sut.EnqueueAsync<TestJob>(null, CancellationToken.None);

        var payload = JsonSerializer.Deserialize<JobDispatchPayload>(capturedTicker!.Request);
        payload!.SerializedData.Should().BeNull();

        var progress = await _db.JobProgress.FindAsync(capturedTicker.Id);
        progress!.Data.Should().BeNull();
    }

    [Fact]
    public async Task EnqueueAsync_SetsExecutionTimeToNow()
    {
        var before = DateTime.UtcNow;
        TimeTickerEntity? capturedTicker = null;
        _ = _timeManager.AddAsync(Arg.Do<TimeTickerEntity>(t => capturedTicker = t));

        await _sut.EnqueueAsync<TestJob>(null, CancellationToken.None);

        capturedTicker!.ExecutionTime.Should().BeOnOrAfter(before);
        capturedTicker.ExecutionTime.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    [Fact]
    public async Task EnqueueAsync_StoresSerializedDataInProgress()
    {
        var data = new TestData("import-csv", 500);

        var jobId = await _sut.EnqueueAsync<TestJob>(data, CancellationToken.None);

        var progress = await _db.JobProgress.FindAsync(jobId.Value);
        progress!.Data.Should().NotBeNull();
        var deserialized = JsonSerializer.Deserialize<TestData>(progress.Data!);
        deserialized!.Name.Should().Be("import-csv");
        deserialized.Count.Should().Be(500);
    }

    // --- ScheduleAsync ---

    [Fact]
    public async Task ScheduleAsync_CreatesTimeTickerWithFutureTime()
    {
        var executeAt = DateTimeOffset.UtcNow.AddHours(2);
        TimeTickerEntity? capturedTicker = null;
        _ = _timeManager.AddAsync(Arg.Do<TimeTickerEntity>(t => capturedTicker = t));

        var jobId = await _sut.ScheduleAsync<TestJob>(executeAt, new TestData("scheduled", 1), CancellationToken.None);

        jobId.Value.Should().NotBeEmpty();
        capturedTicker!.ExecutionTime.Should().BeCloseTo(executeAt.UtcDateTime, TimeSpan.FromSeconds(1));

        var progress = await _db.JobProgress.FindAsync(jobId.Value);
        progress.Should().NotBeNull();
    }

    [Fact]
    public async Task ScheduleAsync_WithNullData_Works()
    {
        var executeAt = DateTimeOffset.UtcNow.AddDays(1);

        var jobId = await _sut.ScheduleAsync<TestJob>(executeAt, null, CancellationToken.None);

        jobId.Value.Should().NotBeEmpty();
        await _timeManager.Received(1).AddAsync(Arg.Any<TimeTickerEntity>());
    }

    // --- AddRecurringAsync ---

    [Fact]
    public async Task AddRecurringAsync_CreatesCronTicker()
    {
        CronTickerEntity? capturedTicker = null;
        _ = _cronManager.AddAsync(Arg.Do<CronTickerEntity>(t => capturedTicker = t));

        var id = await _sut.AddRecurringAsync<TestJob>("cleanup", "0 2 * * *", null, CancellationToken.None);

        id.Value.Should().NotBeEmpty();
        capturedTicker.Should().NotBeNull();
        capturedTicker!.Expression.Should().Be("0 2 * * *");
        capturedTicker.Description.Should().Be("cleanup");
        capturedTicker.IsEnabled.Should().BeTrue();
        capturedTicker.Function.Should().Be("ModuleJobDispatcher");
    }

    [Fact]
    public async Task AddRecurringAsync_WithData_SerializesPayload()
    {
        CronTickerEntity? capturedTicker = null;
        _ = _cronManager.AddAsync(Arg.Do<CronTickerEntity>(t => capturedTicker = t));

        await _sut.AddRecurringAsync<TestJob>("sync", "*/5 * * * *", new TestData("sync", 10), CancellationToken.None);

        var payload = JsonSerializer.Deserialize<JobDispatchPayload>(capturedTicker!.Request);
        payload!.SerializedData.Should().NotBeNull();
    }

    // --- RemoveRecurringAsync ---

    [Fact]
    public async Task RemoveRecurringAsync_DelegatesToCronManager()
    {
        var id = RecurringJobId.From(Guid.NewGuid());

        await _sut.RemoveRecurringAsync(id, CancellationToken.None);

        await _cronManager.Received(1).DeleteAsync(id.Value);
    }

    // --- CancelAsync ---

    [Fact]
    public async Task CancelAsync_DelegatesToTimeManager()
    {
        var id = JobId.From(Guid.NewGuid());

        await _sut.CancelAsync(id, CancellationToken.None);

        await _timeManager.Received(1).DeleteAsync(id.Value);
    }

    // --- GetStatusAsync ---

    [Fact]
    public async Task GetStatusAsync_NonExistentJob_ReturnsNull()
    {
        var result = await _sut.GetStatusAsync(JobId.From(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    // --- Test Helpers ---

    private sealed class TestJob : IModuleJob
    {
        public Task ExecuteAsync(IJobExecutionContext context, CancellationToken ct) =>
            Task.CompletedTask;
    }

    private record TestData(string Name, int Count);
}
