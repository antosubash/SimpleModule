using FluentAssertions;
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

    [Fact]
    public void PolicyImplementor_GeneratesScopedRegistration()
    {
        var compilation = GeneratorTestHelper.CreateCompilation(PolicyForDtoSource);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var moduleExt = result
            .GeneratedTrees.First(t =>
                t.FilePath.EndsWith("ModuleExtensions.g.cs", StringComparison.Ordinal)
            )
            .GetText()
            .ToString();

        moduleExt
            .Should()
            .Contain(
                "services.AddScoped<global::SimpleModule.Core.Authorization.Policies.IPolicy<global::TestApp.Product>, global::TestApp.ProductPolicy>();"
            );
    }

    [Fact]
    public void PolicyForDtoResource_DoesNotReportSm0058()
    {
        var compilation = GeneratorTestHelper.CreateCompilation(PolicyForDtoSource);
        var (_, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(compilation);

        diagnostics.Should().NotContain(d => d.Id == "SM0058");
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

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var moduleExt = result
            .GeneratedTrees.First(t =>
                t.FilePath.EndsWith("ModuleExtensions.g.cs", StringComparison.Ordinal)
            )
            .GetText()
            .ToString();

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

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var moduleExt = result
            .GeneratedTrees.First(t =>
                t.FilePath.EndsWith("ModuleExtensions.g.cs", StringComparison.Ordinal)
            )
            .GetText()
            .ToString();

        moduleExt.Should().NotContain("Auto-discovered resource policies");
    }
}
