using FluentAssertions;
using SimpleModule.Tenants.Contracts;

namespace Tenants.Tests.Unit;

public class TenantIdTests
{
    [Fact]
    public void From_CreatesValueObject()
    {
        var id = TenantId.From(42);

        id.Value.Should().Be(42);
    }

    [Fact]
    public void Equals_WithSameValue_ReturnsTrue()
    {
        var id1 = TenantId.From(1);
        var id2 = TenantId.From(1);

        id1.Should().Be(id2);
    }

    [Fact]
    public void Equals_WithDifferentValue_ReturnsFalse()
    {
        var id1 = TenantId.From(1);
        var id2 = TenantId.From(2);

        id1.Should().NotBe(id2);
    }
}
