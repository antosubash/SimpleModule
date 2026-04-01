using BackgroundJobs.Tests.Helpers;
using FluentAssertions;
using NSubstitute;
using SimpleModule.BackgroundJobs.Contracts;
using SimpleModule.BackgroundJobs.Entities;
using SimpleModule.BackgroundJobs.Services;
using TickerQ.Utilities.Entities;
using TickerQ.Utilities.Interfaces;

namespace BackgroundJobs.Tests.Unit;

public sealed class BackgroundJobsContractsServiceTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly BackgroundJobsDbContext _db;
    private readonly ITimeTickerManager<TimeTickerEntity> _timeManager =
        Substitute.For<ITimeTickerManager<TimeTickerEntity>>();
    private readonly BackgroundJobsContractsService _sut;

    public BackgroundJobsContractsServiceTests()
    {
        _db = _factory.Create();
        _sut = new BackgroundJobsContractsService(_db, _timeManager);
    }

    public void Dispose()
    {
        _db.Dispose();
        _factory.Dispose();
    }

    // --- GetJobsAsync ---

    [Fact]
    public async Task GetJobsAsync_EmptyDb_ReturnsEmptyResult()
    {
        var result = await _sut.GetJobsAsync(new JobFilter());

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetJobsAsync_DefaultFilter_ReturnsPaginatedResults()
    {
        var result = await _sut.GetJobsAsync(new JobFilter { Page = 1, PageSize = 10 });

        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    // --- GetJobDetailAsync ---

    [Fact]
    public async Task GetJobDetailAsync_NonExistentId_ReturnsNull()
    {
        var result = await _sut.GetJobDetailAsync(JobId.From(Guid.NewGuid()));

        result.Should().BeNull();
    }

    // --- GetRecurringJobsAsync ---

    [Fact]
    public async Task GetRecurringJobsAsync_EmptyDb_ReturnsEmptyList()
    {
        var result = await _sut.GetRecurringJobsAsync();

        result.Should().BeEmpty();
    }

    // --- RetryAsync ---

    [Fact]
    public async Task RetryAsync_NonExistentJob_ThrowsInvalidOperation()
    {
        var act = () => _sut.RetryAsync(JobId.From(Guid.NewGuid()));

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not found*");
    }

    // --- Helper ---

    private static JobProgress CreateProgress(Guid id, string jobType = "TestJob", string module = "Test")
    {
        return new JobProgress
        {
            Id = id,
            JobTypeName = jobType,
            ModuleName = module,
            ProgressPercentage = 0,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
    }
}
