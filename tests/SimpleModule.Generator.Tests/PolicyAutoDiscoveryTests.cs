using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SimpleModule.Generator.Tests.Helpers;

namespace SimpleModule.Generator.Tests;

public class PolicyAutoDiscoveryTests
{
    private const string PolicyForDtoSource = """
        using System.Security.Claims;
        using System.Threading;
        using System.Threading.Tasks;
        using SimpleModule.Core;
        using SimpleModule.Core.Authorization.Policies;
        using Microsoft.Extensions.DependencyInjection;

        namespace TestApp
        {
            [Module("Products")]
            public class ProductsModule : IModule
            {
                public void ConfigureServices(IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration) { }
            }

            [Dto]
            public class Product
            {
                public string OwnerId { get; set; } = "";
            }

            public sealed class ProductPolicy : IPolicy<Product>
            {
                public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, string action, Product resource, CancellationToken cancellationToken = default) =>
                    Task.FromResult(AuthorizationResult.Allow());
            }
        }
        """;

    private static string GetModuleExtensions(CSharpCompilation compilation)
    {
        var result = GeneratorTestHelper.RunGenerator(compilation);
        return result
            .GeneratedTrees.First(t =>
                t.FilePath.EndsWith("ModuleExtensions.g.cs", StringComparison.Ordinal)
            )
            .GetText()
            .ToString();
    }

    [Fact]
    public void PolicyImplementor_GeneratesTryAddEnumerableRegistration()
    {
        var moduleExt = GetModuleExtensions(
            GeneratorTestHelper.CreateCompilation(PolicyForDtoSource)
        );

        moduleExt
            .Should()
            .Contain(
                "services.TryAddEnumerable(global::Microsoft.Extensions.DependencyInjection.ServiceDescriptor.Scoped<global::SimpleModule.Core.Authorization.Policies.IPolicy<global::TestApp.Product>, global::TestApp.ProductPolicy>());"
            );
    }

    [Fact]
    public void PolicyForDtoResource_DoesNotReportPolicyDiagnostics()
    {
        var compilation = GeneratorTestHelper.CreateCompilation(PolicyForDtoSource);
        var (_, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(compilation);

        diagnostics
            .Should()
            .NotContain(d => d.Id == "SM0058" || d.Id == "SM0059" || d.Id == "SM0060");
    }

    [Fact]
    public void PolicyForNonDtoResource_ReportsSm0058()
    {
        var source = """
            using System.Security.Claims;
            using System.Threading;
            using System.Threading.Tasks;
            using SimpleModule.Core;
            using SimpleModule.Core.Authorization.Policies;
            using Microsoft.Extensions.DependencyInjection;

            namespace TestApp
            {
                [Module("Products")]
                public class ProductsModule : IModule
                {
                    public void ConfigureServices(IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration) { }
                }

                // Not a [Dto] and not in a .Contracts assembly
                public class InternalProduct
                {
                    public string OwnerId { get; set; } = "";
                }

                public sealed class InternalProductPolicy : IPolicy<InternalProduct>
                {
                    public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, string action, InternalProduct resource, CancellationToken cancellationToken = default) =>
                        Task.FromResult(AuthorizationResult.Allow());
                }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var (_, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(compilation);

        var sm0058 = diagnostics.Where(d => d.Id == "SM0058").ToList();
        sm0058.Should().ContainSingle();
        sm0058[0].GetMessage(null).Should().Contain("InternalProductPolicy");
        sm0058[0].GetMessage(null).Should().Contain("InternalProduct");
    }

    [Fact]
    public void NonPublicPolicy_ReportsSm0059AndIsNotRegistered()
    {
        var source = """
            using System.Security.Claims;
            using System.Threading;
            using System.Threading.Tasks;
            using SimpleModule.Core;
            using SimpleModule.Core.Authorization.Policies;
            using Microsoft.Extensions.DependencyInjection;

            namespace TestApp
            {
                [Module("Products")]
                public class ProductsModule : IModule
                {
                    public void ConfigureServices(IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration) { }
                }

                [Dto]
                public class Product
                {
                    public string OwnerId { get; set; } = "";
                }

                internal sealed class HiddenPolicy : IPolicy<Product>
                {
                    public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, string action, Product resource, CancellationToken cancellationToken = default) =>
                        Task.FromResult(AuthorizationResult.Allow());
                }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var (result, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(compilation);

        diagnostics.Where(d => d.Id == "SM0059").Should().ContainSingle();
        var moduleExt = result
            .GeneratedTrees.First(t =>
                t.FilePath.EndsWith("ModuleExtensions.g.cs", StringComparison.Ordinal)
            )
            .GetText()
            .ToString();
        moduleExt.Should().NotContain("HiddenPolicy");
    }

    [Fact]
    public void NestedPolicy_IsDiscoveredAndRegistered()
    {
        var source = """
            using System.Security.Claims;
            using System.Threading;
            using System.Threading.Tasks;
            using SimpleModule.Core;
            using SimpleModule.Core.Authorization.Policies;
            using Microsoft.Extensions.DependencyInjection;

            namespace TestApp
            {
                [Module("Products")]
                public class ProductsModule : IModule
                {
                    public void ConfigureServices(IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration) { }
                }

                [Dto]
                public class Product
                {
                    public string OwnerId { get; set; } = "";
                }

                public class ProductFeature
                {
                    public sealed class NestedPolicy : IPolicy<Product>
                    {
                        public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, string action, Product resource, CancellationToken cancellationToken = default) =>
                            Task.FromResult(AuthorizationResult.Allow());
                    }
                }
            }
            """;

        var moduleExt = GetModuleExtensions(GeneratorTestHelper.CreateCompilation(source));

        moduleExt.Should().Contain("global::TestApp.ProductFeature.NestedPolicy");
    }

    [Fact]
    public void AbstractPolicy_IsNotRegistered()
    {
        var source = """
            using System.Security.Claims;
            using System.Threading;
            using System.Threading.Tasks;
            using SimpleModule.Core;
            using SimpleModule.Core.Authorization.Policies;
            using Microsoft.Extensions.DependencyInjection;

            namespace TestApp
            {
                [Module("Products")]
                public class ProductsModule : IModule
                {
                    public void ConfigureServices(IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration) { }
                }

                [Dto]
                public class Product
                {
                    public string OwnerId { get; set; } = "";
                }

                public abstract class BasePolicy : IPolicy<Product>
                {
                    public abstract Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, string action, Product resource, CancellationToken cancellationToken = default);
                }
            }
            """;

        var moduleExt = GetModuleExtensions(GeneratorTestHelper.CreateCompilation(source));

        moduleExt.Should().NotContain("BasePolicy");
    }

    [Fact]
    public void NoPolicies_OmitsPolicySection()
    {
        var source = """
            using SimpleModule.Core;
            using Microsoft.Extensions.DependencyInjection;

            namespace TestApp
            {
                [Module("Products")]
                public class ProductsModule : IModule
                {
                    public void ConfigureServices(IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration) { }
                }
            }
            """;

        var moduleExt = GetModuleExtensions(GeneratorTestHelper.CreateCompilation(source));

        moduleExt.Should().NotContain("Auto-discovered resource policies");
    }

    // --- Multi-assembly scenarios -------------------------------------------------

    /// <summary>
    /// Compiles one or more contracts assemblies, then a host assembly referencing all
    /// of them (each contracts source may also reference the previously compiled ones).
    /// </summary>
    private static CSharpCompilation CreateMultiAssemblyCompilation(
        (string AssemblyName, string Source)[] contractsAssemblies,
        string hostSource
    )
    {
        var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        var baseRefs = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
            MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Runtime.dll")),
            MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Collections.dll")),
            MetadataReference.CreateFromFile(typeof(SimpleModule.Core.IModule).Assembly.Location),
            MetadataReference.CreateFromFile(
                typeof(System.Security.Claims.ClaimsPrincipal).Assembly.Location
            ),
            MetadataReference.CreateFromFile(
                typeof(Microsoft.Extensions.DependencyInjection.IServiceCollection)
                    .Assembly
                    .Location
            ),
            MetadataReference.CreateFromFile(
                typeof(Microsoft.Extensions.Configuration.IConfiguration).Assembly.Location
            ),
        };

        var contractsRefs = new List<MetadataReference>();
        foreach (var (assemblyName, source) in contractsAssemblies)
        {
            var compilation = CSharpCompilation.Create(
                assemblyName,
                [CSharpSyntaxTree.ParseText(source)],
                [.. baseRefs, .. contractsRefs],
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            );

            using var ms = new MemoryStream();
            var emit = compilation.Emit(ms);
            emit.Success.Should()
                .BeTrue(
                    $"contracts assembly {assemblyName} should compile. Diagnostics: "
                        + string.Join(
                            ", ",
                            emit.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)
                        )
                );
            contractsRefs.Add(MetadataReference.CreateFromImage(ms.ToArray()));
        }

        var hostRefs = new List<MetadataReference>([.. baseRefs, .. contractsRefs]);
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

        return CSharpCompilation.Create(
            "TestAssembly",
            [CSharpSyntaxTree.ParseText(hostSource)],
            hostRefs,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );
    }

    [Fact]
    public void PolicyInContractsAssembly_IsDiscoveredAndPassesSm0058()
    {
        // Resource has no [Dto] — living in the .Contracts assembly is sufficient
        // (covers [NoDtoGeneration]/IEvent entities excluded from DtoTypes).
        var contractsSource = """
            using System.Security.Claims;
            using System.Threading;
            using System.Threading.Tasks;
            using SimpleModule.Core.Authorization.Policies;

            namespace TestApp.Contracts
            {
                public class Product
                {
                    public string OwnerId { get; set; } = "";
                }

                public sealed class ProductPolicy : IPolicy<Product>
                {
                    public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, string action, Product resource, CancellationToken cancellationToken = default) =>
                        Task.FromResult(AuthorizationResult.Allow());
                }
            }
            """;

        var hostSource = """
            using SimpleModule.Core;
            using Microsoft.Extensions.DependencyInjection;

            namespace TestApp
            {
                [Module("Products")]
                public class ProductsModule : IModule
                {
                    public void ConfigureServices(IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration) { }
                }
            }
            """;

        var compilation = CreateMultiAssemblyCompilation(
            [("TestAssembly.Contracts", contractsSource)],
            hostSource
        );
        var (result, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(compilation);

        diagnostics
            .Should()
            .NotContain(d => d.Id == "SM0058" || d.Id == "SM0059" || d.Id == "SM0060");
        var moduleExt = result
            .GeneratedTrees.First(t =>
                t.FilePath.EndsWith("ModuleExtensions.g.cs", StringComparison.Ordinal)
            )
            .GetText()
            .ToString();
        moduleExt.Should().Contain("global::TestApp.Contracts.ProductPolicy");
    }

    [Fact]
    public void PolicyForForeignModuleResource_ReportsSm0060()
    {
        // The Products module lives in its own assembly so its contracts assembly maps
        // to module "Products"; the host assembly hosts the Notifications module with a
        // policy targeting the Products resource — a foreign policy.
        var productsModule = """
            using SimpleModule.Core;
            using Microsoft.Extensions.DependencyInjection;

            namespace Products
            {
                [Module("Products")]
                public class ProductsModule : IModule
                {
                    public void ConfigureServices(IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration) { }
                }
            }
            """;

        var productsContracts = """
            namespace Products.Contracts
            {
                public class Product
                {
                    public string OwnerId { get; set; } = "";
                }
            }
            """;

        var hostSource = """
            using System.Security.Claims;
            using System.Threading;
            using System.Threading.Tasks;
            using SimpleModule.Core;
            using SimpleModule.Core.Authorization.Policies;
            using Microsoft.Extensions.DependencyInjection;

            namespace TestApp
            {
                [Module("Notifications")]
                public class NotificationsModule : IModule
                {
                    public void ConfigureServices(IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration) { }
                }

                public sealed class ForeignProductPolicy : IPolicy<Products.Contracts.Product>
                {
                    public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, string action, Products.Contracts.Product resource, CancellationToken cancellationToken = default) =>
                        Task.FromResult(AuthorizationResult.Deny("foreign veto"));
                }
            }
            """;

        var compilation = CreateMultiAssemblyCompilation(
            [("Products", productsModule), ("Products.Contracts", productsContracts)],
            hostSource
        );
        var (_, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(compilation);

        var sm0060 = diagnostics.Where(d => d.Id == "SM0060").ToList();
        sm0060.Should().ContainSingle();
        sm0060[0].GetMessage(null).Should().Contain("ForeignProductPolicy");
        sm0060[0].GetMessage(null).Should().Contain("Products");
    }
}
