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
    )
    {
        var coreRef = MetadataReference.CreateFromFile(
            typeof(SimpleModule.Core.IModule).Assembly.Location
        );
        var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;

        var baseRefs = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
            MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Runtime.dll")),
            MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Collections.dll")),
            coreRef,
        };

        // Compile the contracts assembly
        var contractsTree = CSharpSyntaxTree.ParseText(contractsSource);
        var contractsCompilation = CSharpCompilation.Create(
            contractsAssemblyName,
            [contractsTree],
            baseRefs,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        using var ms = new MemoryStream();
        var emitResult = contractsCompilation.Emit(ms);
        emitResult
            .Success.Should()
            .BeTrue(
                "contracts assembly should compile without errors. Diagnostics: "
                    + string.Join(
                        ", ",
                        emitResult
                            .Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)
                            .Select(d => d.ToString())
                    )
            );
        ms.Seek(0, SeekOrigin.Begin);
        var contractsRef = MetadataReference.CreateFromImage(ms.ToArray());

        var hostRefs = new List<MetadataReference>(baseRefs) { contractsRef };

        var aspNetDir = Path.GetDirectoryName(
            typeof(Microsoft.Extensions.DependencyInjection.IServiceCollection).Assembly.Location
        );
        if (aspNetDir is not null)
        {
            var diAbstractions = Path.Combine(
                aspNetDir,
                "Microsoft.Extensions.DependencyInjection.Abstractions.dll"
            );
            if (File.Exists(diAbstractions))
                hostRefs.Add(MetadataReference.CreateFromFile(diAbstractions));
        }

        hostRefs.Add(
            MetadataReference.CreateFromFile(
                typeof(Microsoft.Extensions.Configuration.IConfiguration).Assembly.Location
            )
        );

        hostRefs.Add(
            MetadataReference.CreateFromFile(
                typeof(Microsoft.AspNetCore.Http.IResult).Assembly.Location
            )
        );

        var hostTree = CSharpSyntaxTree.ParseText(hostSource);
        return CSharpCompilation.Create(
            "TestAssembly",
            [hostTree],
            hostRefs,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );
    }

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
