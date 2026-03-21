using FluentAssertions;
using SimpleModule.Generator.Tests.Helpers;

namespace SimpleModule.Generator.Tests;

public class DependencyDiagnosticTests
{
    [Fact]
    public void SM0010_NoCycle_NoDiagnostic()
    {
        var source = """
            using SimpleModule.Core;

            namespace TestApp.Products
            {
                [Module("Products")]
                public class ProductsModule : IModule { }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var (_, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(compilation);

        diagnostics.Should().NotContain(d => d.Id == "SM0010");
    }

    [Fact]
    public void SM0011_NoIllegalRef_NoDiagnostic()
    {
        var source = """
            using SimpleModule.Core;

            namespace TestApp.Products
            {
                [Module("Products")]
                public class ProductsModule : IModule { }
            }

            namespace TestApp.Orders
            {
                [Module("Orders")]
                public class OrdersModule : IModule { }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var (_, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(compilation);

        // Single-assembly compilation — no cross-module impl references
        diagnostics.Should().NotContain(d => d.Id == "SM0011");
    }

    [Fact]
    public void SM0012_SmallInterface_NoDiagnostic()
    {
        var source = """
            using SimpleModule.Core;

            namespace TestApp.Products
            {
                [Module("Products")]
                public class ProductsModule : IModule { }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var (_, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(compilation);

        diagnostics.Should().NotContain(d => d.Id == "SM0012");
        diagnostics.Should().NotContain(d => d.Id == "SM0013");
    }

    [Fact]
    public void SM0014_NoContractsReference_NoDiagnostic()
    {
        var source = """
            using SimpleModule.Core;

            namespace TestApp.Products
            {
                [Module("Products")]
                public class ProductsModule : IModule { }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var (_, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(compilation);

        diagnostics.Should().NotContain(d => d.Id == "SM0014");
    }

    [Fact]
    public void NoDependencyDiagnostics_ForValidSetup()
    {
        var source = """
            using SimpleModule.Core;
            using Microsoft.Extensions.DependencyInjection;

            namespace TestApp.Products
            {
                [Module("Products")]
                public class ProductsModule : IModule
                {
                    public void ConfigureServices(IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration) { }
                }
            }

            namespace TestApp.Orders
            {
                [Module("Orders")]
                public class OrdersModule : IModule
                {
                    public void ConfigureServices(IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration) { }
                }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var (_, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(compilation);

        diagnostics
            .Where(d => d.Id.StartsWith("SM001", System.StringComparison.Ordinal))
            .Should()
            .BeEmpty("valid setup should produce no SM001x diagnostics");
    }
}
