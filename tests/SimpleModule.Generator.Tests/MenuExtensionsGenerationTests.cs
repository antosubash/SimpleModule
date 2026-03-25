using FluentAssertions;
using SimpleModule.Generator.Tests.Helpers;

namespace SimpleModule.Generator.Tests;

public class MenuExtensionsGenerationTests
{
    [Fact]
    public void ModuleWithConfigureMenu_GeneratesMenuExtensionsFile()
    {
        var source = """
            using SimpleModule.Core;
            using SimpleModule.Core.Menu;

            namespace TestApp;

            [Module("Test")]
            public class TestModule : IModule
            {
                public void ConfigureMenu(IMenuBuilder menus) { }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        result
            .GeneratedTrees.Should()
            .Contain(t => t.FilePath.EndsWith("MenuExtensions.g.cs", StringComparison.Ordinal));
    }

    [Fact]
    public void ModuleWithConfigureMenu_GeneratedCodeCallsConfigureMenu()
    {
        var source = """
            using SimpleModule.Core;
            using SimpleModule.Core.Menu;

            namespace TestApp;

            [Module("Test")]
            public class TestModule : IModule
            {
                public void ConfigureMenu(IMenuBuilder menus) { }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var menuExt = result
            .GeneratedTrees.First(t =>
                t.FilePath.EndsWith("MenuExtensions.g.cs", StringComparison.Ordinal)
            )
            .GetText()
            .ToString();

        menuExt
            .Should()
            .Contain(
                "((global::SimpleModule.Core.IModule)ModuleExtensions.s_TestApp_TestModule).ConfigureMenu(menus)"
            );
        menuExt.Should().Contain("CollectModuleMenuItems");
        menuExt.Should().Contain("new MenuBuilder()");
        menuExt.Should().Contain("new MenuRegistry(menus.ToList())");
    }

    [Fact]
    public void ModuleWithoutConfigureMenu_GeneratesMenuExtensionsWithNoMenuCalls()
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

        var menuExt = result
            .GeneratedTrees.First(t =>
                t.FilePath.EndsWith("MenuExtensions.g.cs", StringComparison.Ordinal)
            )
            .GetText()
            .ToString();

        menuExt.Should().Contain("CollectModuleMenuItems");
        menuExt.Should().NotContain(".ConfigureMenu(menus)");
    }

    [Fact]
    public void MultipleModules_OnlySomeWithMenu_CallsOnlyThoseWithMenu()
    {
        var source = """
            using SimpleModule.Core;
            using SimpleModule.Core.Menu;
            using Microsoft.Extensions.DependencyInjection;
            using Microsoft.AspNetCore.Routing;

            namespace TestApp;

            [Module("WithMenu")]
            public class WithMenuModule : IModule
            {
                public void ConfigureMenu(IMenuBuilder menus) { }
                public void ConfigureEndpoints(IEndpointRouteBuilder endpoints) { }
            }

            [Module("NoMenu")]
            public class NoMenuModule : IModule
            {
                public void ConfigureServices(IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration) { }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var menuExt = result
            .GeneratedTrees.First(t =>
                t.FilePath.EndsWith("MenuExtensions.g.cs", StringComparison.Ordinal)
            )
            .GetText()
            .ToString();

        menuExt
            .Should()
            .Contain(
                "((global::SimpleModule.Core.IModule)ModuleExtensions.s_TestApp_WithMenuModule).ConfigureMenu(menus)"
            );
        menuExt.Should().NotContain("NoMenuModule.ConfigureMenu");
    }

    [Fact]
    public void GeneratedMenuExtensions_RegistersSingletonMenuRegistry()
    {
        var source = """
            using SimpleModule.Core;
            using SimpleModule.Core.Menu;

            namespace TestApp;

            [Module("Test")]
            public class TestModule : IModule
            {
                public void ConfigureMenu(IMenuBuilder menus) { }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var menuExt = result
            .GeneratedTrees.First(t =>
                t.FilePath.EndsWith("MenuExtensions.g.cs", StringComparison.Ordinal)
            )
            .GetText()
            .ToString();

        menuExt
            .Should()
            .Contain("services.AddSingleton<IMenuRegistry>(new MenuRegistry(menus.ToList()))");
    }

    [Fact]
    public void GeneratedMenuExtensions_HasCorrectNamespaceAndUsings()
    {
        var source = """
            using SimpleModule.Core;
            using SimpleModule.Core.Menu;

            namespace TestApp;

            [Module("Test")]
            public class TestModule : IModule
            {
                public void ConfigureMenu(IMenuBuilder menus) { }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var menuExt = result
            .GeneratedTrees.First(t =>
                t.FilePath.EndsWith("MenuExtensions.g.cs", StringComparison.Ordinal)
            )
            .GetText()
            .ToString();

        menuExt.Should().Contain("using Microsoft.Extensions.DependencyInjection;");
        menuExt.Should().Contain("using SimpleModule.Core.Menu;");
        menuExt.Should().Contain("namespace SimpleModule.Core;");
        menuExt.Should().Contain("public static class MenuExtensions");
    }

    [Fact]
    public void MultipleModulesWithMenu_AllGetConfigureMenuCalls()
    {
        var source = """
            using SimpleModule.Core;
            using SimpleModule.Core.Menu;

            namespace TestApp;

            [Module("First")]
            public class FirstModule : IModule
            {
                public void ConfigureMenu(IMenuBuilder menus) { }
            }

            [Module("Second")]
            public class SecondModule : IModule
            {
                public void ConfigureMenu(IMenuBuilder menus) { }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var menuExt = result
            .GeneratedTrees.First(t =>
                t.FilePath.EndsWith("MenuExtensions.g.cs", StringComparison.Ordinal)
            )
            .GetText()
            .ToString();

        menuExt
            .Should()
            .Contain(
                "((global::SimpleModule.Core.IModule)ModuleExtensions.s_TestApp_FirstModule).ConfigureMenu(menus)"
            );
        menuExt
            .Should()
            .Contain(
                "((global::SimpleModule.Core.IModule)ModuleExtensions.s_TestApp_SecondModule).ConfigureMenu(menus)"
            );
    }

    [Fact]
    public void ModuleWithAllMethods_GeneratesAllExtensionFiles()
    {
        var source = """
            using SimpleModule.Core;
            using SimpleModule.Core.Menu;
            using Microsoft.Extensions.DependencyInjection;
            using Microsoft.AspNetCore.Routing;

            namespace TestApp;

            [Module("Full")]
            public class FullModule : IModule
            {
                public void ConfigureServices(IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration) { }
                public void ConfigureEndpoints(IEndpointRouteBuilder endpoints) { }
                public void ConfigureMenu(IMenuBuilder menus) { }
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
        result
            .GeneratedTrees.Should()
            .Contain(t => t.FilePath.EndsWith("MenuExtensions.g.cs", StringComparison.Ordinal));
    }
}
