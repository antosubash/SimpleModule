using FluentAssertions;
using SimpleModule.Tenants.Contracts;

namespace Tenants.Tests.Unit;

public class TenantHostIdTests
{
    [Fact]
    public void From_CreatesValueObject()
    {
        var id = TenantHostId.From(42);

        id.Value.Should().Be(42);
    }

    [Fact]
    public void Equals_WithSameValue_ReturnsTrue()
    {
        var id1 = TenantHostId.From(1);
        var id2 = TenantHostId.From(1);

        id1.Should().Be(id2);
    }
}
