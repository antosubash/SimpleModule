using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SimpleModule.Generator.Tests.Helpers;

namespace SimpleModule.Generator.Tests;

public class ContractAutoDiscoveryTests
{
    /// <summary>
    /// Helper: compiles a "contracts" assembly (named {assemblyName}) from source,
    /// then creates the host compilation ("TestAssembly") that references it.
    /// This mirrors the real layout where a module references its own *.Contracts assembly.
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

        // Get the contracts assembly as a reference
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

        // Now compile the host assembly referencing contracts
        var hostRefs = new List<MetadataReference>(baseRefs) { contractsRef };

        // Add DI abstractions
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

        // Add ASP.NET Core HTTP abstractions (for IResult)
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
    public void SingleImplementation_GeneratesAddScoped()
    {
        var contractsSource = """
            namespace TestAssembly.Contracts
            {
                public interface IProductContracts
                {
                    void DoSomething();
                }
            }
            """;

        var hostSource = """
            using SimpleModule.Core;
            using Microsoft.Extensions.DependencyInjection;
            using TestAssembly.Contracts;

            namespace TestAssembly
            {
                [Module("TestAssembly")]
                public class TestAssemblyModule : IModule
                {
                    public void ConfigureServices(IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration) { }
                }

                public class ProductService : IProductContracts
                {
                    public void DoSomething() { }
                }
            }
            """;

        var compilation = CreateMultiAssemblyCompilation(contractsSource, hostSource);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var moduleExt = result
            .GeneratedTrees.First(t =>
                t.FilePath.EndsWith("ModuleExtensions.g.cs", StringComparison.Ordinal)
            )
            .GetText()
            .ToString();

        moduleExt.Should().Contain("AddScoped<");
        moduleExt.Should().Contain("IProductContracts");
        moduleExt.Should().Contain("ProductService");
    }

    [Fact]
    public void NoImplementation_EmitsSM0025()
    {
        var contractsSource = """
            namespace TestAssembly.Contracts
            {
                public interface IProductContracts
                {
                    void DoSomething();
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

                // No implementation of IProductContracts
            }
            """;

        var compilation = CreateMultiAssemblyCompilation(contractsSource, hostSource);
        var (_, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(compilation);

        diagnostics.Should().Contain(d => d.Id == "SM0025");
        var diag = diagnostics.First(d => d.Id == "SM0025");
        diag.GetMessage(System.Globalization.CultureInfo.InvariantCulture)
            .Should()
            .Contain("IProductContracts");
    }

    [Fact]
    public void MultipleImplementations_EmitsSM0026()
    {
        var contractsSource = """
            namespace TestAssembly.Contracts
            {
                public interface IProductContracts
                {
                    void DoSomething();
                }
            }
            """;

        var hostSource = """
            using SimpleModule.Core;
            using Microsoft.Extensions.DependencyInjection;
            using TestAssembly.Contracts;

            namespace TestAssembly
            {
                [Module("TestAssembly")]
                public class TestAssemblyModule : IModule
                {
                    public void ConfigureServices(IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration) { }
                }

                public class ProductServiceA : IProductContracts
                {
                    public void DoSomething() { }
                }

                public class ProductServiceB : IProductContracts
                {
                    public void DoSomething() { }
                }
            }
            """;

        var compilation = CreateMultiAssemblyCompilation(contractsSource, hostSource);
        var (_, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(compilation);

        diagnostics.Should().Contain(d => d.Id == "SM0026");
        var diag = diagnostics.First(d => d.Id == "SM0026");
        var message = diag.GetMessage(System.Globalization.CultureInfo.InvariantCulture);
        message.Should().Contain("IProductContracts");
        message.Should().Contain("ProductServiceA");
        message.Should().Contain("ProductServiceB");
    }

    [Fact]
    public void InternalImplementation_EmitsSM0028()
    {
        var contractsSource = """
            namespace TestAssembly.Contracts
            {
                public interface IProductContracts
                {
                    void DoSomething();
                }
            }
            """;

        var hostSource = """
            using SimpleModule.Core;
            using Microsoft.Extensions.DependencyInjection;
            using TestAssembly.Contracts;

            namespace TestAssembly
            {
                [Module("TestAssembly")]
                public class TestAssemblyModule : IModule
                {
                    public void ConfigureServices(IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration) { }
                }

                internal class ProductService : IProductContracts
                {
                    public void DoSomething() { }
                }
            }
            """;

        var compilation = CreateMultiAssemblyCompilation(contractsSource, hostSource);
        var (_, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(compilation);

        diagnostics.Should().Contain(d => d.Id == "SM0028");
        var diag = diagnostics.First(d => d.Id == "SM0028");
        var message = diag.GetMessage(System.Globalization.CultureInfo.InvariantCulture);
        message.Should().Contain("ProductService");
        message.Should().Contain("IProductContracts");
    }

    [Fact]
    public void AbstractImplementation_EmitsSM0029()
    {
        var contractsSource = """
            namespace TestAssembly.Contracts
            {
                public interface IProductContracts
                {
                    void DoSomething();
                }
            }
            """;

        var hostSource = """
            using SimpleModule.Core;
            using Microsoft.Extensions.DependencyInjection;
            using TestAssembly.Contracts;

            namespace TestAssembly
            {
                [Module("TestAssembly")]
                public class TestAssemblyModule : IModule
                {
                    public void ConfigureServices(IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration) { }
                }

                public abstract class ProductServiceBase : IProductContracts
                {
                    public abstract void DoSomething();
                }
            }
            """;

        var compilation = CreateMultiAssemblyCompilation(contractsSource, hostSource);
        var (_, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(compilation);

        diagnostics.Should().Contain(d => d.Id == "SM0029");
        var diag = diagnostics.First(d => d.Id == "SM0029");
        var message = diag.GetMessage(System.Globalization.CultureInfo.InvariantCulture);
        message.Should().Contain("ProductServiceBase");
        message.Should().Contain("IProductContracts");
    }
}
