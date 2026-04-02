using FluentAssertions;
using SimpleModule.BackgroundJobs.Contracts;

namespace BackgroundJobs.Tests.Unit;

public sealed class JobIdTests
{
    [Fact]
    public void From_WithGuid_CreatesJobId()
    {
        var guid = Guid.NewGuid();

        var id = JobId.From(guid);

        id.Value.Should().Be(guid);
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        var guid = Guid.NewGuid();

        var id1 = JobId.From(guid);
        var id2 = JobId.From(guid);

        id1.Should().Be(id2);
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var id1 = JobId.From(Guid.NewGuid());
        var id2 = JobId.From(Guid.NewGuid());

        id1.Should().NotBe(id2);
    }
}

public sealed class RecurringJobIdTests
{
    [Fact]
    public void From_WithGuid_CreatesRecurringJobId()
    {
        var guid = Guid.NewGuid();

        var id = RecurringJobId.From(guid);

        id.Value.Should().Be(guid);
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        var guid = Guid.NewGuid();

        var id1 = RecurringJobId.From(guid);
        var id2 = RecurringJobId.From(guid);

        id1.Should().Be(id2);
    }
}
