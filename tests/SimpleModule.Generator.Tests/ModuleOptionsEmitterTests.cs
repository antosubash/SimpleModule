using FluentAssertions;
using SimpleModule.Generator.Tests.Helpers;

namespace SimpleModule.Generator.Tests;

public class ModuleOptionsEmitterTests
{
    [Fact]
    public void Module_WithOptionsClass_GeneratesConfigureExtensionMethod()
    {
        var source = """
            using SimpleModule.Core;

            namespace TestApp
            {
                [Module("Products", RoutePrefix = "/api/products")]
                public class ProductsModule : IModule { }
            }

            namespace TestApp
            {
                public class ProductsModuleOptions : IModuleOptions
                {
                    public int MaxPageSize { get; set; } = 100;
                }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var generated = GetGeneratedSource(result, "ModuleOptionsExtensions.g.cs");

        generated.Should().Contain("ConfigureProducts(this SimpleModuleOptions options");
        generated.Should().Contain("global::TestApp.ProductsModuleOptions");
        generated.Should().Contain("options.ConfigureModule(configure)");
    }

    [Fact]
    public void Module_WithOptionsClass_RegistersDefaultsInRegisterMethod()
    {
        var source = """
            using SimpleModule.Core;

            namespace TestApp
            {
                [Module("Products", RoutePrefix = "/api/products")]
                public class ProductsModule : IModule { }

                public class ProductsModuleOptions : IModuleOptions
                {
                    public int MaxPageSize { get; set; } = 100;
                }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var generated = GetGeneratedSource(result, "ModuleOptionsExtensions.g.cs");

        generated.Should().Contain("RegisterModuleOptionsDefaults");
        generated.Should().Contain("services.AddOptions<global::TestApp.ProductsModuleOptions>()");
    }

    [Fact]
    public void MultipleModules_InSameAssembly_GeneratesConfigureForDiscoveredOptions()
    {
        // In a single-assembly compilation, ScanModuleAssemblies deduplicates
        // by assembly and discovers options for the first module in that assembly.
        // In production, each module lives in a separate assembly.
        var source = """
            using SimpleModule.Core;

            namespace TestApp
            {
                [Module("Products")]
                public class ProductsModule : IModule { }

                public class ProductsModuleOptions : IModuleOptions
                {
                    public int PageSize { get; set; } = 10;
                }

                [Module("Orders")]
                public class OrdersModule : IModule { }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var generated = GetGeneratedSource(result, "ModuleOptionsExtensions.g.cs");

        // Options class is discovered and a Configure method is generated
        generated.Should().Contain("ConfigureProducts(");
        generated.Should().Contain("ProductsModuleOptions");
    }

    [Fact]
    public void Module_WithNoOptionsClass_GeneratesEmptyExtensions()
    {
        var source = """
            using SimpleModule.Core;

            namespace TestApp
            {
                [Module("Products")]
                public class ProductsModule : IModule { }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var generated = GetGeneratedSource(result, "ModuleOptionsExtensions.g.cs");

        generated.Should().Contain("ModuleOptionsExtensions");
        generated.Should().NotContain("ConfigureProducts(");
    }

    [Fact]
    public void SM0044_MultipleOptionsForSameModule_ReportsDiagnostic()
    {
        var source = """
            using SimpleModule.Core;

            namespace TestApp
            {
                [Module("Products")]
                public class ProductsModule : IModule { }

                public class ProductsModuleOptions : IModuleOptions
                {
                    public int PageSize { get; set; }
                }

                public class ProductsExtraOptions : IModuleOptions
                {
                    public bool Feature { get; set; }
                }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var (_, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(compilation);

        diagnostics.Should().Contain(d => d.Id == "SM0044");
    }

    [Fact]
    public void SM0044_SingleOptionsPerModule_NoDiagnostic()
    {
        var source = """
            using SimpleModule.Core;

            namespace TestApp
            {
                [Module("Products")]
                public class ProductsModule : IModule { }

                public class ProductsModuleOptions : IModuleOptions
                {
                    public int PageSize { get; set; }
                }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var (_, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(compilation);

        diagnostics.Should().NotContain(d => d.Id == "SM0044");
    }

    [Fact]
    public void HostingExtensions_CallsApplyModuleOptions()
    {
        var source = """
            using SimpleModule.Core;

            namespace TestApp
            {
                [Module("Products")]
                public class ProductsModule : IModule { }

                public class ProductsModuleOptions : IModuleOptions
                {
                    public int PageSize { get; set; }
                }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var generated = GetGeneratedSource(result, "HostingExtensions.g.cs");

        generated.Should().Contain("ApplyModuleOptions");
    }

    private static string GetGeneratedSource(
        Microsoft.CodeAnalysis.GeneratorDriverRunResult result,
        string fileName
    )
    {
        return result
            .GeneratedTrees.First(t => t.FilePath.EndsWith(fileName, StringComparison.Ordinal))
            .GetText()
            .ToString();
    }
}
