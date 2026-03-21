using FluentAssertions;
using SimpleModule.Generator.Tests.Helpers;

namespace SimpleModule.Generator.Tests;

public class DependencyInferenceTests
{
    [Fact]
    public void SingleAssembly_TwoModules_NoDependencies()
    {
        var source = """
            using SimpleModule.Core;

            namespace TestApp.ModuleA
            {
                [Module("ModuleA")]
                public class ModuleAModule : IModule { }
            }

            namespace TestApp.ModuleB
            {
                [Module("ModuleB")]
                public class ModuleBModule : IModule { }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var (_, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(compilation);

        // In single-assembly compilation, no cross-module references exist
        diagnostics.Should().NotContain(d => d.Id == "SM0010");
        diagnostics.Should().NotContain(d => d.Id == "SM0011");
        diagnostics.Should().NotContain(d => d.Id == "SM0014");
    }

    [Fact]
    public void SingleAssembly_NoFalseDependencyDiagnostics()
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

        // No SM001x diagnostics should fire
        diagnostics
            .Where(d => d.Id.StartsWith("SM001", StringComparison.Ordinal))
            .Should()
            .BeEmpty("single-assembly compilation should not trigger dependency diagnostics");
    }
}
