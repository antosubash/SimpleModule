using FluentAssertions;
using SimpleModule.Generator.Tests.Helpers;

namespace SimpleModule.Generator.Tests;

public class ModuleDiscovererGeneratorTests
{
    [Fact]
    public void ModuleWithBothMethods_GeneratesBothExtensionFiles()
    {
        var source = """
            using SimpleModule.Core;
            using Microsoft.Extensions.DependencyInjection;
            using Microsoft.AspNetCore.Routing;

            namespace TestApp;

            [Module("Test")]
            public class TestModule : IModule
            {
                public void ConfigureServices(IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration) { }
                public void ConfigureEndpoints(IEndpointRouteBuilder endpoints) { }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        result
            .GeneratedTrees.Should()
            .Contain(t => t.FilePath.EndsWith("ModuleExtensions.g.cs", StringComparison.Ordinal));
        result
            .GeneratedTrees.Should()
            .Contain(t => t.FilePath.EndsWith("EndpointExtensions.g.cs", StringComparison.Ordinal));

        var moduleExt = result
            .GeneratedTrees.First(t =>
                t.FilePath.EndsWith("ModuleExtensions.g.cs", StringComparison.Ordinal)
            )
            .GetText()
            .ToString();
        moduleExt.Should().Contain("s_TestApp_TestModule");
        moduleExt.Should().Contain("global::TestApp.TestModule");
        moduleExt.Should().Contain("((global::SimpleModule.Core.IModule)s_TestApp_TestModule).ConfigureServices(services, configuration)");

        var endpointExt = result
            .GeneratedTrees.First(t =>
                t.FilePath.EndsWith("EndpointExtensions.g.cs", StringComparison.Ordinal)
            )
            .GetText()
            .ToString();
        endpointExt.Should().Contain("((global::SimpleModule.Core.IModule)ModuleExtensions.s_TestApp_TestModule).ConfigureEndpoints(app)");
    }

    [Fact]
    public void ModuleWithOnlyEndpoints_SkipsConfigureServicesCall()
    {
        var source = """
            using SimpleModule.Core;
            using Microsoft.AspNetCore.Routing;

            namespace TestApp;

            [Module("EndpointOnly")]
            public class EndpointOnlyModule : IModule
            {
                public void ConfigureEndpoints(IEndpointRouteBuilder endpoints) { }
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
        moduleExt.Should().NotContain("EndpointOnlyModule().ConfigureServices");

        var endpointExt = result
            .GeneratedTrees.First(t =>
                t.FilePath.EndsWith("EndpointExtensions.g.cs", StringComparison.Ordinal)
            )
            .GetText()
            .ToString();
        endpointExt.Should().Contain("((global::SimpleModule.Core.IModule)ModuleExtensions.s_TestApp_EndpointOnlyModule).ConfigureEndpoints(app)");
    }

    [Fact]
    public void Module_WithPartialOverrides_EmitsCallsThroughInterface()
    {
        // A module that only overrides ConfigureServices but NOT ConfigureMenu or ConfigurePermissions.
        // The generator must NOT emit direct calls to methods the class doesn't override
        // (they're default interface methods). All calls go through ((IModule)...) casts
        // so both overridden and default methods work correctly.
        var source = """
            using SimpleModule.Core;
            using Microsoft.Extensions.DependencyInjection;
            using Microsoft.Extensions.Configuration;

            namespace TestApp;

            [Module("Partial")]
            public class PartialModule : IModule
            {
                public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
                {
                    // Only this method is overridden
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

        // ConfigureServices should be emitted through the interface
        moduleExt
            .Should()
            .Contain(
                "((global::SimpleModule.Core.IModule)s_TestApp_PartialModule).ConfigureServices(services, configuration)"
            );

        // All method calls must go through the IModule interface cast, never directly on the concrete type.
        // This ensures default interface implementations are callable even when the class doesn't override them.
        moduleExt.Should().NotContainAny(
            "s_TestApp_PartialModule.ConfigureServices(",
            "s_TestApp_PartialModule.ConfigureMenu(",
            "s_TestApp_PartialModule.ConfigurePermissions(",
            "s_TestApp_PartialModule.ConfigureEndpoints(",
            "s_TestApp_PartialModule.ConfigureMiddleware(",
            "s_TestApp_PartialModule.ConfigureSettings("
        );
    }

    [Fact]
    public void NoModules_EmitsNothing()
    {
        var source = """
            namespace TestApp;

            public class NotAModule { }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        result.GeneratedTrees.Should().BeEmpty();
    }

    [Fact]
    public void ModuleWithDtoTypes_AlsoEmitsJsonResolver()
    {
        var source = """
            using SimpleModule.Core;
            using Microsoft.Extensions.DependencyInjection;
            using Microsoft.AspNetCore.Routing;

            namespace TestApp;

            [Module("Test")]
            public class TestModule : IModule
            {
                public void ConfigureServices(IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration) { }
                public void ConfigureEndpoints(IEndpointRouteBuilder endpoints) { }
            }

            [Dto]
            public class MyDto
            {
                public int Id { get; set; }
                public string Name { get; set; } = "";
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        result
            .GeneratedTrees.Should()
            .Contain(t =>
                t.FilePath.EndsWith("ModulesJsonResolver.g.cs", StringComparison.Ordinal)
            );
    }

    [Fact]
    public void MultipleModules_AllAppearInGeneratedCode()
    {
        var source = """
            using SimpleModule.Core;
            using Microsoft.Extensions.DependencyInjection;
            using Microsoft.AspNetCore.Routing;

            namespace TestApp;

            [Module("First")]
            public class FirstModule : IModule
            {
                public void ConfigureServices(IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration) { }
                public void ConfigureEndpoints(IEndpointRouteBuilder endpoints) { }
            }

            [Module("Second")]
            public class SecondModule : IModule
            {
                public void ConfigureServices(IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration) { }
                public void ConfigureEndpoints(IEndpointRouteBuilder endpoints) { }
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
        moduleExt.Should().Contain("FirstModule");
        moduleExt.Should().Contain("SecondModule");
    }
}
