using BackgroundJobs.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SimpleModule.BackgroundJobs.Scheduler;

namespace SimpleModule.BackgroundJobs.Tests.Scheduler;

public sealed class DatabaseInstanceLeaderTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();

    [Fact]
    public async Task FirstAcquirer_Wins()
    {
        using var db = _factory.Create();
        var leader = new DatabaseInstanceLeader(db, NullLogger<DatabaseInstanceLeader>.Instance);
        (await leader.TryAcquireAsync("scheduler", "host-A", TimeSpan.FromMinutes(1)))
            .Should()
            .BeTrue();
    }

    [Fact]
    public async Task SecondAcquirer_Fails_WhileLeaseHeld()
    {
        using var db1 = _factory.Create();
        using var db2 = _factory.Create();
        var l1 = new DatabaseInstanceLeader(db1, NullLogger<DatabaseInstanceLeader>.Instance);
        var l2 = new DatabaseInstanceLeader(db2, NullLogger<DatabaseInstanceLeader>.Instance);

        (await l1.TryAcquireAsync("scheduler", "host-A", TimeSpan.FromMinutes(1))).Should().BeTrue();
        (await l2.TryAcquireAsync("scheduler", "host-B", TimeSpan.FromMinutes(1))).Should().BeFalse();
    }

    [Fact]
    public async Task SameOwner_RenewsLease()
    {
        using var db = _factory.Create();
        var leader = new DatabaseInstanceLeader(db, NullLogger<DatabaseInstanceLeader>.Instance);
        (await leader.TryAcquireAsync("scheduler", "host-A", TimeSpan.FromMinutes(1))).Should().BeTrue();
        (await leader.TryAcquireAsync("scheduler", "host-A", TimeSpan.FromMinutes(1))).Should().BeTrue();
    }

    [Fact]
    public async Task NewOwner_TakesOver_AfterExpiry()
    {
        using var db = _factory.Create();
        var leader = new DatabaseInstanceLeader(db, NullLogger<DatabaseInstanceLeader>.Instance);
        (await leader.TryAcquireAsync("scheduler", "host-A", TimeSpan.FromMilliseconds(1))).Should().BeTrue();
        await Task.Delay(50);
        (await leader.TryAcquireAsync("scheduler", "host-B", TimeSpan.FromMinutes(1))).Should().BeTrue();
    }

    public void Dispose()
    {
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }
}
