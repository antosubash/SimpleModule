using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SimpleModule.Generator.Tests.Helpers;

namespace SimpleModule.Generator.Tests;

public class DtoConventionTests
{
    /// <summary>
    /// Helper: compiles a "contracts" assembly (named {assemblyName}) from source,
    /// then creates the host compilation ("TestAssembly") that references it.
    /// </summary>
    private static CSharpCompilation CreateMultiAssemblyCompilation(
        string contractsSource,
        string hostSource,
        string contractsAssemblyName = "TestAssembly.Contracts"
    ) =>
        GeneratorTestHelper.CreateMultiAssemblyCompilation(
            [(contractsAssemblyName, contractsSource)],
            hostSource
        );

    [Fact]
    public void PublicContractsType_IncludedInTypeScript()
    {
        var contractsSource = """
            namespace TestAssembly.Contracts
            {
                public class ProductDto
                {
                    public int Id { get; set; }
                    public string Name { get; set; } = "";
                }
            }
            """;

        var hostSource = """
            using SimpleModule.Core;
            using Microsoft.Extensions.DependencyInjection;

            namespace TestAssembly
            {
                [Module("TestAssembly")]
                public class TestAssemblyModule : IModule
                {
                    public void ConfigureServices(IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration) { }
                }
            }
            """;

        var compilation = CreateMultiAssemblyCompilation(contractsSource, hostSource);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        // Convention DTO should produce TypeScript definitions
        // Module name from FQN "global::TestAssembly.Contracts.ProductDto" -> parts[1] = "Contracts"
        var tsTree = result.GeneratedTrees.FirstOrDefault(t =>
            t.FilePath.EndsWith("DtoTypeScript_Contracts.g.cs", StringComparison.Ordinal)
        );
        tsTree.Should().NotBeNull("convention DTO should generate TypeScript definitions");

        var tsOutput = tsTree!.GetText().ToString();
        tsOutput.Should().Contain("ProductDto");
    }

    [Fact]
    public void NoDtoGenerationAttribute_Excluded()
    {
        var contractsSource = """
            using SimpleModule.Core;

            namespace TestAssembly.Contracts
            {
                public class ProductDto
                {
                    public int Id { get; set; }
                    public string Name { get; set; } = "";
                }

                [NoDtoGeneration]
                public class InternalHelper
                {
                    public int Value { get; set; }
                }
            }
            """;

        var hostSource = """
            using SimpleModule.Core;
            using Microsoft.Extensions.DependencyInjection;

            namespace TestAssembly
            {
                [Module("TestAssembly")]
                public class TestAssemblyModule : IModule
                {
                    public void ConfigureServices(IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration) { }
                }
            }
            """;

        var compilation = CreateMultiAssemblyCompilation(contractsSource, hostSource);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        // Find TypeScript output
        var tsTree = result.GeneratedTrees.FirstOrDefault(t =>
            t.FilePath.EndsWith("DtoTypeScript_Contracts.g.cs", StringComparison.Ordinal)
        );
        tsTree.Should().NotBeNull("ProductDto should generate TypeScript definitions");

        var tsOutput = tsTree!.GetText().ToString();
        tsOutput.Should().Contain("ProductDto");
        tsOutput.Should().NotContain("InternalHelper");
    }

    [Fact]
    public void InterfaceInContracts_Excluded()
    {
        var contractsSource = """
            namespace TestAssembly.Contracts
            {
                public interface IProductContracts
                {
                    void DoSomething();
                }

                public class ProductDto
                {
                    public int Id { get; set; }
                }
            }
            """;

        var hostSource = """
            using SimpleModule.Core;
            using Microsoft.Extensions.DependencyInjection;

            namespace TestAssembly
            {
                [Module("TestAssembly")]
                public class TestAssemblyModule : IModule
                {
                    public void ConfigureServices(IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration) { }
                }
            }
            """;

        var compilation = CreateMultiAssemblyCompilation(contractsSource, hostSource);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        // TypeScript output should not contain the interface as a DTO
        var tsTree = result.GeneratedTrees.FirstOrDefault(t =>
            t.FilePath.EndsWith("DtoTypeScript_Contracts.g.cs", StringComparison.Ordinal)
        );
        tsTree.Should().NotBeNull("ProductDto should produce TypeScript definitions");

        var tsOutput = tsTree!.GetText().ToString();
        // Interface should not be treated as a DTO
        tsOutput.Should().NotContain("IProductContracts");
        // But the class should be present
        tsOutput.Should().Contain("ProductDto");
    }

    [Fact]
    public void ExplicitDtoAttribute_StillWorks()
    {
        var source = """
            using SimpleModule.Core;
            using Microsoft.Extensions.DependencyInjection;

            namespace TestApp.Contracts
            {
                [Module("Test")]
                public class TestModule : IModule
                {
                    public void ConfigureServices(IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration) { }
                }

                [Dto]
                public class ItemDto
                {
                    public int Id { get; set; }
                    public string Name { get; set; } = "";
                }
            }
            """;

        // Single-assembly compilation — no Contracts assembly needed for [Dto]
        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var moduleExt = result
            .GeneratedTrees.First(t =>
                t.FilePath.EndsWith("ModuleExtensions.g.cs", StringComparison.Ordinal)
            )
            .GetText()
            .ToString();

        // Explicit [Dto] should still configure JSON resolver
        moduleExt.Should().Contain("ConfigureHttpJsonOptions");
        moduleExt.Should().Contain("ModulesJsonResolver");

        // TypeScript definitions should be generated (module name from FQN -> "Contracts")
        var tsTree = result.GeneratedTrees.FirstOrDefault(t =>
            t.FilePath.Contains("DtoTypeScript_", StringComparison.Ordinal)
        );
        tsTree.Should().NotBeNull("[Dto] type should generate TypeScript definitions");

        var tsOutput = tsTree!.GetText().ToString();
        tsOutput.Should().Contain("ItemDto");
    }
}
