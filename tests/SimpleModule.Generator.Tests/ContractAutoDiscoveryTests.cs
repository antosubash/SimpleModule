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
    ) =>
        GeneratorTestHelper.CreateMultiAssemblyCompilation(
            [(contractsAssemblyName, contractsSource)],
            hostSource
        );

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
    public void ManualImplementations_DoNotEmitContractDiagnostics()
    {
        // A module with a provider-swappable contract ships several implementations
        // and registers one conditionally in ConfigureServices. Marking each with
        // [ManualContractRegistration] must suppress SM0026 (multiple impls) and
        // SM0028 (impl must be public) while still satisfying the contract so SM0025
        // (no impl) does not fire. Mirrors the Users/OpenIddict provider design (#236).
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

                [ManualContractRegistration]
                public sealed class LocalProductService : IProductContracts
                {
                    public void DoSomething() { }
                }

                [ManualContractRegistration]
                internal sealed class ExternalProductService : IProductContracts
                {
                    public void DoSomething() { }
                }
            }
            """;

        var compilation = CreateMultiAssemblyCompilation(contractsSource, hostSource);
        var (_, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(compilation);

        diagnostics.Should().NotContain(d => d.Id == "SM0025");
        diagnostics.Should().NotContain(d => d.Id == "SM0026");
        diagnostics.Should().NotContain(d => d.Id == "SM0028");
    }

    [Fact]
    public void ManualImplementation_IsNotAutoRegistered()
    {
        // A [ManualContractRegistration] implementation must not be auto-wired by the
        // generator — its module registers it itself, so a generated AddScoped would
        // double-register (or pick the wrong provider).
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

                [ManualContractRegistration]
                public sealed class ProductService : IProductContracts
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

        moduleExt.Should().NotContain("ProductService");
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
