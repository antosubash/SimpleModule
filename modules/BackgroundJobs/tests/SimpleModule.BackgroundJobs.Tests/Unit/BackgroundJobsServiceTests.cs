using BackgroundJobs.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SimpleModule.BackgroundJobs.Contracts;
using SimpleModule.BackgroundJobs.Services;

namespace BackgroundJobs.Tests.Unit;

public sealed class BackgroundJobsServiceTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly BackgroundJobsDbContext _db;
    private readonly IJobQueue _queue = Substitute.For<IJobQueue>();
    private readonly BackgroundJobsService _sut;

    public BackgroundJobsServiceTests()
    {
        _db = _factory.Create();
        _sut = new BackgroundJobsService(
            _queue,
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
    public async Task EnqueueAsync_EnqueuesViaQueueAndCreatesProgress()
    {
        var jobId = await _sut.EnqueueAsync<TestJob>(new TestData("import", 100), CancellationToken.None);

        jobId.Value.Should().NotBeEmpty();
        await _queue.Received(1).EnqueueAsync(Arg.Any<JobQueueEntry>(), Arg.Any<CancellationToken>());

        var progress = await _db.JobProgress.FindAsync(jobId.Value);
        progress.Should().NotBeNull();
        progress!.ProgressPercentage.Should().Be(0);
        progress.JobTypeName.Should().Contain("TestJob");
    }

    [Fact]
    public async Task EnqueueAsync_WithNullData_SerializesNullPayloadData()
    {
        JobQueueEntry? capturedEntry = null;
        _ = _queue.EnqueueAsync(Arg.Do<JobQueueEntry>(e => capturedEntry = e), Arg.Any<CancellationToken>());

        await _sut.EnqueueAsync<TestJob>(null, CancellationToken.None);

        capturedEntry!.SerializedData.Should().BeNull();
        capturedEntry.JobTypeName.Should().Contain("TestJob");
    }

    [Fact]
    public async Task EnqueueAsync_StoresSerializedDataInProgress()
    {
        var data = new TestData("import-csv", 500);

        var jobId = await _sut.EnqueueAsync<TestJob>(data, CancellationToken.None);

        var progress = await _db.JobProgress.FindAsync(jobId.Value);
        progress!.Data.Should().NotBeNull();
        progress.Data.Should().Contain("import-csv");
    }

    // --- ScheduleAsync ---

    [Fact]
    public async Task ScheduleAsync_EnqueuesWithFutureScheduledAt()
    {
        var executeAt = DateTimeOffset.UtcNow.AddHours(2);
        JobQueueEntry? capturedEntry = null;
        _ = _queue.EnqueueAsync(Arg.Do<JobQueueEntry>(e => capturedEntry = e), Arg.Any<CancellationToken>());

        var jobId = await _sut.ScheduleAsync<TestJob>(executeAt, new TestData("scheduled", 1), CancellationToken.None);

        jobId.Value.Should().NotBeEmpty();
        capturedEntry!.ScheduledAt.Should().BeCloseTo(executeAt, TimeSpan.FromSeconds(1));
        var progress = await _db.JobProgress.FindAsync(jobId.Value);
        progress.Should().NotBeNull();
    }

    [Fact]
    public async Task ScheduleAsync_WithNullData_Works()
    {
        var executeAt = DateTimeOffset.UtcNow.AddDays(1);

        var jobId = await _sut.ScheduleAsync<TestJob>(executeAt, null, CancellationToken.None);

        jobId.Value.Should().NotBeEmpty();
        await _queue.Received(1).EnqueueAsync(Arg.Any<JobQueueEntry>(), Arg.Any<CancellationToken>());
    }

    // --- AddRecurringAsync ---

    [Fact]
    public async Task AddRecurringAsync_EnqueuesWithCronFields()
    {
        JobQueueEntry? capturedEntry = null;
        _ = _queue.EnqueueAsync(Arg.Do<JobQueueEntry>(e => capturedEntry = e), Arg.Any<CancellationToken>());

        var id = await _sut.AddRecurringAsync<TestJob>("cleanup", "0 2 * * *", null, CancellationToken.None);

        id.Value.Should().NotBeEmpty();
        capturedEntry.Should().NotBeNull();
        capturedEntry!.CronExpression.Should().Be("0 2 * * *");
        capturedEntry.RecurringName.Should().Be("cleanup");
    }

    // --- CancelAsync ---

    [Fact]
    public async Task CancelAsync_NonExistent_DoesNotThrow()
    {
        var id = JobId.From(Guid.NewGuid());
        var act = () => _sut.CancelAsync(id, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    // --- GetStatusAsync ---

    [Fact]
    public async Task GetStatusAsync_NonExistentJob_ReturnsNull()
    {
        var result = await _sut.GetStatusAsync(JobId.From(Guid.NewGuid()), CancellationToken.None);
        result.Should().BeNull();
    }

    // --- MapQueueState ---

    [Theory]
    [InlineData(JobQueueEntryState.Pending, JobState.Pending)]
    [InlineData(JobQueueEntryState.Claimed, JobState.Running)]
    [InlineData(JobQueueEntryState.Completed, JobState.Completed)]
    [InlineData(JobQueueEntryState.Failed, JobState.Failed)]
    public void MapQueueState_MapsCorrectly(JobQueueEntryState input, JobState expected)
    {
        var result = BackgroundJobsService.MapQueueState(input);
        result.Should().Be(expected);
    }

    // --- Test Helpers ---

    private sealed class TestJob : IModuleJob
    {
        public Task ExecuteAsync(IJobExecutionContext context, CancellationToken ct) =>
            Task.CompletedTask;
    }

    private record TestData(string Name, int Count);
}
