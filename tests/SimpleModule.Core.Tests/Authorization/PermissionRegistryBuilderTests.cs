using FluentAssertions;
using SimpleModule.Core.Authorization;

namespace SimpleModule.Core.Tests.Authorization;

public class PermissionRegistryBuilderTests
{
    private sealed class TestPermissions
    {
        public const string View = "Test.View";
        public const string Create = "Test.Create";
    }

    [Fact]
    public void AddPermissions_ScansStaticConstants()
    {
        var builder = new PermissionRegistryBuilder();
        builder.AddPermissions<TestPermissions>();
        var registry = builder.Build();

        registry.AllPermissions.Should().Contain("Test.View");
        registry.AllPermissions.Should().Contain("Test.Create");
    }

    [Fact]
    public void Build_GroupsByModule()
    {
        var builder = new PermissionRegistryBuilder();
        builder.AddPermission("Products.View");
        builder.AddPermission("Products.Create");
        builder.AddPermission("Orders.View");
        var registry = builder.Build();

        registry.ByModule.Should().ContainKey("Products");
        registry.ByModule["Products"].Should().HaveCount(2);
        registry.ByModule.Should().ContainKey("Orders");
        registry.ByModule["Orders"].Should().HaveCount(1);
    }

    [Fact]
    public void AddPermission_DeduplicatesValues()
    {
        var builder = new PermissionRegistryBuilder();
        builder.AddPermission("Products.View");
        builder.AddPermission("Products.View");
        var registry = builder.Build();

        registry.AllPermissions.Should().HaveCount(1);
    }
}
