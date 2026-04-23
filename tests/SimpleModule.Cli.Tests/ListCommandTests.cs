using System.Reflection;
using FluentAssertions;
using SimpleModule.Cli.Commands.List;
using SimpleModule.Cli.Infrastructure;

namespace SimpleModule.Cli.Tests;

public sealed class ListCommandTests : IDisposable
{
    private readonly string _tempDir;

    public ListCommandTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"sm-list-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        File.WriteAllText(Path.Combine(_tempDir, "Test.slnx"), "<Solution />");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private void WriteModuleClass(string moduleName, string content)
    {
        var dir = Path.Combine(_tempDir, "src", "modules", moduleName, "src", moduleName);
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, $"{moduleName}Module.cs"), content);
    }

    private void WriteConstantsClass(string moduleName, string routePrefix)
    {
        var dir = Path.Combine(_tempDir, "src", "modules", moduleName, "src", moduleName);
        Directory.CreateDirectory(dir);
        File.WriteAllText(
            Path.Combine(dir, $"{moduleName}Constants.cs"),
            $$"""
            public static class {{moduleName}}Constants
            {
                public const string RoutePrefix = "{{routePrefix}}";
            }
            """
        );
    }

    private void WriteEndpointFile(string moduleName, string endpointName)
    {
        var dir = Path.Combine(
            _tempDir,
            "src",
            "modules",
            moduleName,
            "src",
            moduleName,
            "Endpoints",
            moduleName
        );
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, $"{endpointName}Endpoint.cs"), "// endpoint");
    }

    private static string? InvokeReadRoutePrefix(SolutionContext solution, string module)
    {
        var method = typeof(ListCommand).GetMethod(
            "ReadRoutePrefix",
            BindingFlags.NonPublic | BindingFlags.Static
        )!;
        return (string?)method.Invoke(null, [solution, module]);
    }

    private static int InvokeCountEndpoints(SolutionContext solution, string module)
    {
        var method = typeof(ListCommand).GetMethod(
            "CountEndpoints",
            BindingFlags.NonPublic | BindingFlags.Static
        )!;
        return (int)method.Invoke(null, [solution, module])!;
    }

    [Fact]
    public void ReadRoutePrefix_ReturnsLiteral_WhenModuleAttributeHasStringLiteral()
    {
        WriteModuleClass(
            "Products",
            """
            [Module("Products", RoutePrefix = "products")]
            public class ProductsModule : IModule { }
            """
        );
        var solution = SolutionContext.Discover(_tempDir)!;

        InvokeReadRoutePrefix(solution, "Products").Should().Be("products");
    }

    [Fact]
    public void ReadRoutePrefix_ResolvesConstantFromConstantsFile()
    {
        WriteModuleClass(
            "Orders",
            """
            [Module("Orders", RoutePrefix = OrdersConstants.RoutePrefix)]
            public class OrdersModule : IModule { }
            """
        );
        WriteConstantsClass("Orders", "orders");
        var solution = SolutionContext.Discover(_tempDir)!;

        InvokeReadRoutePrefix(solution, "Orders").Should().Be("orders");
    }

    [Fact]
    public void ReadRoutePrefix_ReturnsNull_WhenModuleFileMissing()
    {
        Directory.CreateDirectory(
            Path.Combine(_tempDir, "src", "modules", "Ghost", "src", "Ghost")
        );
        var solution = SolutionContext.Discover(_tempDir)!;

        InvokeReadRoutePrefix(solution, "Ghost").Should().BeNull();
    }

    [Fact]
    public void ReadRoutePrefix_ReturnsNull_WhenAttributeMissing()
    {
        WriteModuleClass(
            "Naked",
            """
            public class NakedModule : IModule { }
            """
        );
        var solution = SolutionContext.Discover(_tempDir)!;

        InvokeReadRoutePrefix(solution, "Naked").Should().BeNull();
    }

    [Fact]
    public void ReadRoutePrefix_ReturnsConstantName_WhenConstantsFileMissing()
    {
        WriteModuleClass(
            "Invoices",
            """
            [Module("Invoices", RoutePrefix = InvoicesConstants.RoutePrefix)]
            public class InvoicesModule : IModule { }
            """
        );
        var solution = SolutionContext.Discover(_tempDir)!;

        InvokeReadRoutePrefix(solution, "Invoices").Should().Be("InvoicesConstants.RoutePrefix");
    }

    [Fact]
    public void CountEndpoints_CountsEndpointSuffixedFilesRecursively()
    {
        WriteEndpointFile("Products", "GetAll");
        WriteEndpointFile("Products", "Create");
        WriteEndpointFile("Products", "Update");

        // A non-endpoint file that should NOT be counted
        var endpointsDir = Path.Combine(
            _tempDir,
            "src",
            "modules",
            "Products",
            "src",
            "Products",
            "Endpoints",
            "Products"
        );
        File.WriteAllText(Path.Combine(endpointsDir, "Helpers.cs"), "// not an endpoint");

        var solution = SolutionContext.Discover(_tempDir)!;

        InvokeCountEndpoints(solution, "Products").Should().Be(3);
    }

    [Fact]
    public void CountEndpoints_ReturnsZero_WhenEndpointsDirMissing()
    {
        Directory.CreateDirectory(
            Path.Combine(_tempDir, "src", "modules", "Empty", "src", "Empty")
        );
        var solution = SolutionContext.Discover(_tempDir)!;

        InvokeCountEndpoints(solution, "Empty").Should().Be(0);
    }
}
