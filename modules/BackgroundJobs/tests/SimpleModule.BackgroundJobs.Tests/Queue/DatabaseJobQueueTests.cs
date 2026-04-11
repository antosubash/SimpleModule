// modules/BackgroundJobs/tests/SimpleModule.BackgroundJobs.Tests/Queue/DatabaseJobQueueTests.cs
using BackgroundJobs.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SimpleModule.BackgroundJobs.Contracts;
using SimpleModule.BackgroundJobs.Queue;

namespace SimpleModule.BackgroundJobs.Tests.Queue;

public sealed class DatabaseJobQueueTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly BackgroundJobsDbContext _db;

    public DatabaseJobQueueTests()
    {
        _db = _factory.Create();
    }

    [Fact]
    public async Task EnqueueAsync_AddsPendingRow()
    {
        var queue = new DatabaseJobQueue(_db, NullLogger<DatabaseJobQueue>.Instance);
        var entry = new JobQueueEntry(
            Guid.NewGuid(),
            "My.Job, Asm",
            """{"x":1}""",
            DateTimeOffset.UtcNow,
            JobQueueEntryState.Pending,
            0,
            null,
            null,
            DateTimeOffset.UtcNow
        );

        await queue.EnqueueAsync(entry);

        var row = await _db.JobQueueEntries.SingleAsync();
        row.State.Should().Be(JobQueueEntryState.Pending);
        row.JobTypeName.Should().Be("My.Job, Asm");
    }

    [Fact]
    public async Task DequeueAsync_ReturnsAndClaimsOldestPending()
    {
        var queue = new DatabaseJobQueue(_db, NullLogger<DatabaseJobQueue>.Instance);
        var older = new JobQueueEntry(
            Guid.NewGuid(),
            "A",
            null,
            DateTimeOffset.UtcNow.AddMinutes(-5),
            JobQueueEntryState.Pending,
            0,
            null,
            null,
            DateTimeOffset.UtcNow.AddMinutes(-5)
        );
        var newer = new JobQueueEntry(
            Guid.NewGuid(),
            "B",
            null,
            DateTimeOffset.UtcNow,
            JobQueueEntryState.Pending,
            0,
            null,
            null,
            DateTimeOffset.UtcNow
        );
        await queue.EnqueueAsync(older);
        await queue.EnqueueAsync(newer);

        var claimed = await queue.DequeueAsync("worker-1");

        claimed.Should().NotBeNull();
        claimed!.JobTypeName.Should().Be("A");
        var claimedId = JobId.From(claimed.Id);
        var row = await _db.JobQueueEntries.SingleAsync(e => e.Id == claimedId);
        row.State.Should().Be(JobQueueEntryState.Claimed);
        row.ClaimedBy.Should().Be("worker-1");
        row.AttemptCount.Should().Be(1);
    }

    [Fact]
    public async Task DequeueAsync_ReturnsNullWhenEmpty()
    {
        var queue = new DatabaseJobQueue(_db, NullLogger<DatabaseJobQueue>.Instance);
        var result = await queue.DequeueAsync("worker-1");
        result.Should().BeNull();
    }

    [Fact]
    public async Task DequeueAsync_IgnoresFutureScheduledEntries()
    {
        var queue = new DatabaseJobQueue(_db, NullLogger<DatabaseJobQueue>.Instance);
        await queue.EnqueueAsync(
            new JobQueueEntry(
                Guid.NewGuid(),
                "Future",
                null,
                DateTimeOffset.UtcNow.AddHours(1),
                JobQueueEntryState.Pending,
                0,
                null,
                null,
                DateTimeOffset.UtcNow
            )
        );

        var result = await queue.DequeueAsync("worker-1");
        result.Should().BeNull();
    }

    [Fact]
    public async Task CompleteAsync_MarksRowCompleted()
    {
        var queue = new DatabaseJobQueue(_db, NullLogger<DatabaseJobQueue>.Instance);
        var id = Guid.NewGuid();
        await queue.EnqueueAsync(
            new JobQueueEntry(
                id,
                "X",
                null,
                DateTimeOffset.UtcNow,
                JobQueueEntryState.Pending,
                0,
                null,
                null,
                DateTimeOffset.UtcNow
            )
        );
        await queue.DequeueAsync("worker-1");

        await queue.CompleteAsync(id);

        var row = await _db.JobQueueEntries.SingleAsync();
        row.State.Should().Be(JobQueueEntryState.Completed);
        row.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task FailAsync_MarksRowFailedWithError()
    {
        var queue = new DatabaseJobQueue(_db, NullLogger<DatabaseJobQueue>.Instance);
        var id = Guid.NewGuid();
        await queue.EnqueueAsync(
            new JobQueueEntry(
                id,
                "X",
                null,
                DateTimeOffset.UtcNow,
                JobQueueEntryState.Pending,
                0,
                null,
                null,
                DateTimeOffset.UtcNow
            )
        );
        await queue.DequeueAsync("worker-1");

        await queue.FailAsync(id, "boom");

        var row = await _db.JobQueueEntries.SingleAsync();
        row.State.Should().Be(JobQueueEntryState.Failed);
        row.Error.Should().Be("boom");
    }

    [Fact]
    public async Task RequeueStalledAsync_MovesStaleClaimedBackToPending()
    {
        var queue = new DatabaseJobQueue(_db, NullLogger<DatabaseJobQueue>.Instance);
        var id = Guid.NewGuid();
        await queue.EnqueueAsync(
            new JobQueueEntry(
                id,
                "X",
                null,
                DateTimeOffset.UtcNow,
                JobQueueEntryState.Pending,
                0,
                null,
                null,
                DateTimeOffset.UtcNow
            )
        );
        await queue.DequeueAsync("worker-1");

        // Manually backdate ClaimedAt to simulate a stall
        var row = await _db.JobQueueEntries.SingleAsync();
        row.ClaimedAt = DateTimeOffset.UtcNow.AddMinutes(-10);
        await _db.SaveChangesAsync();

        var count = await queue.RequeueStalledAsync(TimeSpan.FromMinutes(5));

        count.Should().Be(1);
        var reloaded = await _db.JobQueueEntries.SingleAsync();
        reloaded.State.Should().Be(JobQueueEntryState.Pending);
        reloaded.ClaimedBy.Should().BeNull();
    }

    public void Dispose()
    {
        _db.Dispose();
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }
}
