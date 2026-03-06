using System.Reflection;
using FluentAssertions;
using SimpleModule.Core;

namespace SimpleModule.Core.Tests;

public class ModuleAttributeTests
{
    [Fact]
    public void Constructor_SetsNameAndDefaultVersion()
    {
        var attr = new ModuleAttribute("TestModule");

        attr.Name.Should().Be("TestModule");
        attr.Version.Should().Be("1.0.0");
    }

    [Fact]
    public void Constructor_WithCustomVersion_SetsBothProperties()
    {
        var attr = new ModuleAttribute("X", "2.0.0");

        attr.Name.Should().Be("X");
        attr.Version.Should().Be("2.0.0");
    }

    [Fact]
    public void AttributeUsage_DoesNotAllowMultiple_TargetsClass()
    {
        var usage = typeof(ModuleAttribute).GetCustomAttribute<AttributeUsageAttribute>();

        usage.Should().NotBeNull();
        usage!.AllowMultiple.Should().BeFalse();
        usage.ValidOn.Should().Be(AttributeTargets.Class);
    }
}
