using FluentAssertions;
using SimpleModule.Generator.Tests.Helpers;

namespace SimpleModule.Generator.Tests;

public class ModuleExtensionsGenerationTests
{
    [Fact]
    public void ModuleWithOnlyConfigureServices_SkipsEndpointCall()
    {
        var source = """
            using SimpleModule.Core;
            using Microsoft.Extensions.DependencyInjection;

            namespace TestApp;

            [Module("ServiceOnly")]
            public class ServiceOnlyModule : IModule
            {
                public void ConfigureServices(IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration) { }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var moduleExt = GetGeneratedSource(result, "ModuleExtensions.g.cs");
        moduleExt
            .Should()
            .Contain("s_TestApp_ServiceOnlyModule.ConfigureServices(services, configuration)");

        var endpointExt = GetGeneratedSource(result, "EndpointExtensions.g.cs");
        endpointExt.Should().NotContain("ServiceOnlyModule");
    }

    [Fact]
    public void ModuleWithNestedNamespace_UsesFullyQualifiedName()
    {
        var source = """
            using SimpleModule.Core;
            using Microsoft.Extensions.DependencyInjection;
            using Microsoft.AspNetCore.Routing;

            namespace MyCompany.MyApp.Modules;

            [Module("Nested")]
            public class NestedModule : IModule
            {
                public void ConfigureServices(IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration) { }
                public void ConfigureEndpoints(IEndpointRouteBuilder endpoints) { }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var moduleExt = GetGeneratedSource(result, "ModuleExtensions.g.cs");
        moduleExt.Should().Contain("global::MyCompany.MyApp.Modules.NestedModule");
        moduleExt.Should().Contain("s_MyCompany_MyApp_Modules_NestedModule");
    }

    [Fact]
    public void SharedInstances_AreInternalStaticReadonly()
    {
        var source = """
            using SimpleModule.Core;
            using Microsoft.Extensions.DependencyInjection;

            namespace TestApp;

            [Module("Test")]
            public class TestModule : IModule
            {
                public void ConfigureServices(IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration) { }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var moduleExt = GetGeneratedSource(result, "ModuleExtensions.g.cs");
        moduleExt
            .Should()
            .Contain(
                "internal static readonly global::TestApp.TestModule s_TestApp_TestModule = new();"
            );
    }

    [Fact]
    public void EndpointExtensions_ReferencesModuleExtensionsFields()
    {
        var source = """
            using SimpleModule.Core;
            using Microsoft.AspNetCore.Routing;

            namespace TestApp;

            [Module("Test")]
            public class TestModule : IModule
            {
                public void ConfigureEndpoints(IEndpointRouteBuilder endpoints) { }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var endpointExt = GetGeneratedSource(result, "EndpointExtensions.g.cs");
        endpointExt
            .Should()
            .Contain("ModuleExtensions.s_TestApp_TestModule.ConfigureEndpoints(app)");
    }

    [Fact]
    public void ModuleWithoutIModule_IsIgnored()
    {
        var source = """
            using SimpleModule.Core;

            namespace TestApp;

            [Module("NotAModule")]
            public class NotAModule
            {
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        // The generator discovers by attribute, not by interface.
        // It should still find the class since it has [Module] attribute.
        var moduleExt = GetGeneratedSource(result, "ModuleExtensions.g.cs");
        moduleExt.Should().Contain("NotAModule");
        // But since it doesn't declare ConfigureServices or ConfigureEndpoints,
        // it should not appear in the method bodies.
        moduleExt.Should().NotContain("ConfigureServices(services, configuration)");
    }

    [Fact]
    public void EmptyModule_GeneratesEmptyMethodBodies()
    {
        var source = """
            using SimpleModule.Core;

            namespace TestApp;

            [Module("Empty")]
            public class EmptyModule : IModule { }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var moduleExt = GetGeneratedSource(result, "ModuleExtensions.g.cs");
        // The shared instance field should still be generated
        moduleExt.Should().Contain("s_TestApp_EmptyModule = new()");
        // But no method calls since nothing is declared
        moduleExt.Should().NotContain(".ConfigureServices(");

        var endpointExt = GetGeneratedSource(result, "EndpointExtensions.g.cs");
        endpointExt.Should().NotContain("EmptyModule");
    }

    [Fact]
    public void MultipleModules_EachGetsOwnSharedInstance()
    {
        var source = """
            using SimpleModule.Core;
            using Microsoft.Extensions.DependencyInjection;
            using Microsoft.AspNetCore.Routing;

            namespace TestApp;

            [Module("Alpha")]
            public class AlphaModule : IModule
            {
                public void ConfigureServices(IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration) { }
            }

            [Module("Beta")]
            public class BetaModule : IModule
            {
                public void ConfigureEndpoints(IEndpointRouteBuilder endpoints) { }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var moduleExt = GetGeneratedSource(result, "ModuleExtensions.g.cs");
        moduleExt.Should().Contain("s_TestApp_AlphaModule = new()");
        moduleExt.Should().Contain("s_TestApp_BetaModule = new()");
        // Only Alpha has ConfigureServices
        moduleExt.Should().Contain("s_TestApp_AlphaModule.ConfigureServices");
        moduleExt.Should().NotContain("s_TestApp_BetaModule.ConfigureServices");

        var endpointExt = GetGeneratedSource(result, "EndpointExtensions.g.cs");
        // Only Beta has ConfigureEndpoints
        endpointExt.Should().Contain("s_TestApp_BetaModule.ConfigureEndpoints");
        endpointExt.Should().NotContain("AlphaModule");
    }

    [Fact]
    public void GeneratedFiles_AlwaysIncludeAutoGeneratedComment()
    {
        var source = """
            using SimpleModule.Core;

            namespace TestApp;

            [Module("Test")]
            public class TestModule : IModule { }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        foreach (var tree in result.GeneratedTrees)
        {
            tree.GetText().ToString().Should().StartWith("// <auto-generated/>");
        }
    }

    [Fact]
    public void ModuleExtensions_WithDtos_ConfiguresJsonResolver()
    {
        var source = """
            using SimpleModule.Core;
            using Microsoft.Extensions.DependencyInjection;

            namespace TestApp;

            [Module("Test")]
            public class TestModule : IModule
            {
                public void ConfigureServices(IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration) { }
            }

            [Dto]
            public class ItemDto
            {
                public int Id { get; set; }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var moduleExt = GetGeneratedSource(result, "ModuleExtensions.g.cs");
        moduleExt.Should().Contain("ConfigureHttpJsonOptions");
        moduleExt.Should().Contain("ModulesJsonResolver.Instance");
    }

    [Fact]
    public void ModuleExtensions_WithoutDtos_DoesNotConfigureJsonResolver()
    {
        var source = """
            using SimpleModule.Core;
            using Microsoft.Extensions.DependencyInjection;

            namespace TestApp;

            [Module("Test")]
            public class TestModule : IModule
            {
                public void ConfigureServices(IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration) { }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var moduleExt = GetGeneratedSource(result, "ModuleExtensions.g.cs");
        moduleExt.Should().NotContain("ConfigureHttpJsonOptions");
        moduleExt.Should().NotContain("ModulesJsonResolver");
    }

    [Fact]
    public void RazorComponentExtensions_AlwaysGenerated()
    {
        var source = """
            using SimpleModule.Core;

            namespace TestApp;

            [Module("Test")]
            public class TestModule : IModule { }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        result
            .GeneratedTrees.Should()
            .Contain(t =>
                t.FilePath.EndsWith("RazorComponentExtensions.g.cs", StringComparison.Ordinal)
            );
    }

    [Fact]
    public void GeneratedCode_UsesCorrectNamespace()
    {
        var source = """
            using SimpleModule.Core;

            namespace TestApp;

            [Module("Test")]
            public class TestModule : IModule { }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var moduleExt = GetGeneratedSource(result, "ModuleExtensions.g.cs");
        moduleExt.Should().Contain("namespace SimpleModule.Core;");

        var endpointExt = GetGeneratedSource(result, "EndpointExtensions.g.cs");
        endpointExt.Should().Contain("namespace SimpleModule.Core;");
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
