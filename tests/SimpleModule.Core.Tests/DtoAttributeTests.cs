using System.Reflection;
using FluentAssertions;
using SimpleModule.Core;

namespace SimpleModule.Core.Tests;

public class DtoAttributeTests
{
    [Dto]
    private sealed class TestDtoClass { }

    [Dto]
    private struct TestDtoStruct { }

    [Fact]
    public void DtoAttribute_CanBeAppliedToClass()
    {
        var attr = typeof(TestDtoClass).GetCustomAttribute<DtoAttribute>();
        attr.Should().NotBeNull();
    }

    [Fact]
    public void DtoAttribute_CanBeAppliedToStruct()
    {
        var attr = typeof(TestDtoStruct).GetCustomAttribute<DtoAttribute>();
        attr.Should().NotBeNull();
    }

    [Fact]
    public void AttributeUsage_DoesNotAllowMultiple()
    {
        var usage = typeof(DtoAttribute).GetCustomAttribute<AttributeUsageAttribute>();

        usage.Should().NotBeNull();
        usage!.AllowMultiple.Should().BeFalse();
        usage.ValidOn.Should().HaveFlag(AttributeTargets.Class);
        usage.ValidOn.Should().HaveFlag(AttributeTargets.Struct);
    }
}
