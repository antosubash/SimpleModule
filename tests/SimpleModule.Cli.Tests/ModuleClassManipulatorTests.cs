using FluentAssertions;
using SimpleModule.Cli.Infrastructure;

namespace SimpleModule.Cli.Tests;

public sealed class ModuleClassManipulatorTests : IDisposable
{
    private readonly string _tempDir;

    public ModuleClassManipulatorTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"sm-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    private string CreateModuleFile(string content)
    {
        var path = Path.Combine(_tempDir, "TestModule.cs");
        File.WriteAllText(path, content);
        return path;
    }

    [Fact]
    public void AddFeatureWiring_AddsUsingAndMapCall()
    {
        var path = CreateModuleFile("""
            using SimpleModule.Orders.Features.GetAllOrders;

            namespace SimpleModule.Orders;

            public class OrdersModule : IModule
            {
                public void ConfigureEndpoints(IEndpointRouteBuilder endpoints)
                {
                    var group = endpoints.MapGroup("/api/orders");
                    GetAllOrdersEndpoint.Map(group);
                }
            }
            """);

        var result = ModuleClassManipulator.AddFeatureWiring(path, "Orders", "CreateOrder");

        result.Should().BeTrue();
        var content = File.ReadAllText(path);
        content.Should().Contain("using SimpleModule.Orders.Features.CreateOrder;");
        content.Should().Contain("CreateOrderEndpoint.Map(group);");
    }

    [Fact]
    public void AddFeatureWiring_ReturnsFalse_WhenPatternNotFound()
    {
        var path = CreateModuleFile("""
            namespace SimpleModule.Orders;

            public class OrdersModule
            {
                // No Map(group) pattern
            }
            """);

        var result = ModuleClassManipulator.AddFeatureWiring(path, "Orders", "CreateOrder");

        result.Should().BeFalse();
    }

    [Fact]
    public void AddFeatureWiring_DoesNotDuplicate()
    {
        var path = CreateModuleFile("""
            using SimpleModule.Orders.Features.GetAllOrders;
            using SimpleModule.Orders.Features.CreateOrder;

            namespace SimpleModule.Orders;

            public class OrdersModule : IModule
            {
                public void ConfigureEndpoints(IEndpointRouteBuilder endpoints)
                {
                    var group = endpoints.MapGroup("/api/orders");
                    GetAllOrdersEndpoint.Map(group);
                    CreateOrderEndpoint.Map(group);
                }
            }
            """);

        var result = ModuleClassManipulator.AddFeatureWiring(path, "Orders", "CreateOrder");

        result.Should().BeTrue();
        var content = File.ReadAllText(path);
        var count = content.Split("CreateOrderEndpoint.Map(group);").Length - 1;
        count.Should().Be(1, "should not duplicate the Map call");
    }

    [Fact]
    public void AddFeatureWiring_InsertsAfterLastUsing()
    {
        var path = CreateModuleFile("""
            using Microsoft.AspNetCore.Builder;
            using SimpleModule.Orders.Features.GetAllOrders;

            namespace SimpleModule.Orders;

            public class OrdersModule : IModule
            {
                public void ConfigureEndpoints(IEndpointRouteBuilder endpoints)
                {
                    var group = endpoints.MapGroup("/api/orders");
                    GetAllOrdersEndpoint.Map(group);
                }
            }
            """);

        ModuleClassManipulator.AddFeatureWiring(path, "Orders", "UpdateOrder");

        var lines = File.ReadAllLines(path);
        var usingIndex = Array.FindIndex(lines, l => l.Contains("UpdateOrder", StringComparison.Ordinal) && l.TrimStart().StartsWith("using", StringComparison.Ordinal));
        usingIndex.Should().BeGreaterThan(0, "using should be inserted");

        var mapIndex = Array.FindIndex(lines, l => l.Contains("UpdateOrderEndpoint.Map", StringComparison.Ordinal));
        mapIndex.Should().BeGreaterThan(usingIndex, "Map call should come after using");
    }
}
