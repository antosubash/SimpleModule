using BackgroundJobs.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SimpleModule.BackgroundJobs.Scheduler;

namespace SimpleModule.BackgroundJobs.Tests.Scheduler;

public sealed class DatabaseJobMutexTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();

    [Fact]
    public async Task TryAcquire_Succeeds_OnFirstCall()
    {
        using var db = _factory.Create();
        var mutex = new DatabaseJobMutex(db, NullLogger<DatabaseJobMutex>.Instance);

        var ok = await mutex.TryAcquireAsync("job", "owner-1", TimeSpan.FromMinutes(1));
        ok.Should().BeTrue();
    }

    [Fact]
    public async Task TryAcquire_Fails_WhenHeldByAnotherOwner()
    {
        using var db1 = _factory.Create();
        using var db2 = _factory.Create();

        var m1 = new DatabaseJobMutex(db1, NullLogger<DatabaseJobMutex>.Instance);
        var m2 = new DatabaseJobMutex(db2, NullLogger<DatabaseJobMutex>.Instance);

        (await m1.TryAcquireAsync("job", "owner-1", TimeSpan.FromMinutes(1))).Should().BeTrue();
        (await m2.TryAcquireAsync("job", "owner-2", TimeSpan.FromMinutes(1))).Should().BeFalse();
    }

    [Fact]
    public async Task TryAcquire_SameOwner_RefreshesLease()
    {
        using var db = _factory.Create();
        var mutex = new DatabaseJobMutex(db, NullLogger<DatabaseJobMutex>.Instance);

        (await mutex.TryAcquireAsync("job", "owner-1", TimeSpan.FromMinutes(1))).Should().BeTrue();
        (await mutex.TryAcquireAsync("job", "owner-1", TimeSpan.FromMinutes(1))).Should().BeTrue();
    }

    [Fact]
    public async Task TryAcquire_OtherOwner_CanTakeOverAfterExpiry()
    {
        using var db = _factory.Create();
        var mutex = new DatabaseJobMutex(db, NullLogger<DatabaseJobMutex>.Instance);

        (await mutex.TryAcquireAsync("job", "owner-1", TimeSpan.FromMilliseconds(1))).Should().BeTrue();
        await Task.Delay(50);
        (await mutex.TryAcquireAsync("job", "owner-2", TimeSpan.FromMinutes(1))).Should().BeTrue();
    }

    [Fact]
    public async Task Release_RemovesMutexRow()
    {
        using var db = _factory.Create();
        var mutex = new DatabaseJobMutex(db, NullLogger<DatabaseJobMutex>.Instance);

        await mutex.TryAcquireAsync("job", "owner-1", TimeSpan.FromMinutes(1));
        await mutex.ReleaseAsync("job");

        using var db2 = _factory.Create();
        var m2 = new DatabaseJobMutex(db2, NullLogger<DatabaseJobMutex>.Instance);
        (await m2.TryAcquireAsync("job", "owner-2", TimeSpan.FromMinutes(1))).Should().BeTrue();
    }

    public void Dispose()
    {
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }
}
