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
        moduleExt.Should().Contain("new global::TestApp.TestModule()");

        var endpointExt = result
            .GeneratedTrees.First(t =>
                t.FilePath.EndsWith("EndpointExtensions.g.cs", StringComparison.Ordinal)
            )
            .GetText()
            .ToString();
        endpointExt.Should().Contain("new global::TestApp.TestModule()");
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
        endpointExt.Should().Contain("new global::TestApp.EndpointOnlyModule().ConfigureEndpoints");
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
