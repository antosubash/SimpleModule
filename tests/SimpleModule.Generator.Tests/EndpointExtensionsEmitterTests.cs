using FluentAssertions;
using SimpleModule.Generator.Tests.Helpers;

namespace SimpleModule.Generator.Tests;

public class EndpointExtensionsEmitterTests
{
    [Fact]
    public void Module_WithRoutePrefix_CreatesMapGroupWithTags()
    {
        var source = """
            using Microsoft.AspNetCore.Builder;
            using Microsoft.AspNetCore.Routing;
            using SimpleModule.Core;

            namespace TestApp
            {
                [Module("Products", RoutePrefix = "/api/products")]
                public class ProductsModule : IModule { }
            }

            namespace TestApp.Endpoints
            {
                public class ListEndpoint : IEndpoint
                {
                    public void Map(IEndpointRouteBuilder app)
                    {
                        app.MapGet("/", () => "list");
                    }
                }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var endpointExt = GetGeneratedSource(result, "EndpointExtensions.g.cs");

        endpointExt
            .Should()
            .Contain("var group = app.MapGroup(\"/api/products\").WithTags(\"Products\");");
        endpointExt.Should().Contain("new global::TestApp.Endpoints.ListEndpoint().Map(group);");
    }

    [Fact]
    public void Module_WithoutRoutePrefix_MapsEndpointsDirectlyToApp()
    {
        var source = """
            using Microsoft.AspNetCore.Builder;
            using Microsoft.AspNetCore.Routing;
            using SimpleModule.Core;

            namespace TestApp
            {
                [Module("Misc")]
                public class MiscModule : IModule { }
            }

            namespace TestApp.Endpoints
            {
                public class PingEndpoint : IEndpoint
                {
                    public void Map(IEndpointRouteBuilder app)
                    {
                        app.MapGet("/ping", () => "pong");
                    }
                }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var endpointExt = GetGeneratedSource(result, "EndpointExtensions.g.cs");

        endpointExt.Should().Contain("new global::TestApp.Endpoints.PingEndpoint().Map(app);");
        endpointExt.Should().NotContain("MapGroup");
    }

    [Fact]
    public void Module_WithConfigureEndpoints_SkipsAutoRegistration()
    {
        var source = """
            using Microsoft.AspNetCore.Builder;
            using Microsoft.AspNetCore.Routing;
            using SimpleModule.Core;

            namespace TestApp
            {
                [Module("Custom", RoutePrefix = "/api/custom")]
                public class CustomModule : IModule
                {
                    public void ConfigureEndpoints(IEndpointRouteBuilder endpoints) { }
                }
            }

            namespace TestApp.Endpoints
            {
                public class AutoEndpoint : IEndpoint
                {
                    public void Map(IEndpointRouteBuilder app)
                    {
                        app.MapGet("/auto", () => "auto");
                    }
                }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var endpointExt = GetGeneratedSource(result, "EndpointExtensions.g.cs");

        // Should NOT auto-register endpoints because HasConfigureEndpoints is true
        endpointExt.Should().NotContain("new global::TestApp.Endpoints.AutoEndpoint()");
        // Should use the escape hatch instead
        endpointExt
            .Should()
            .Contain("ModuleExtensions.s_TestApp_CustomModule.ConfigureEndpoints(app);");
    }

    [Fact]
    public void Module_WithConfigureEndpoints_UsesEscapeHatch()
    {
        var source = """
            using Microsoft.AspNetCore.Builder;
            using Microsoft.AspNetCore.Routing;
            using SimpleModule.Core;

            namespace TestApp
            {
                [Module("Manual")]
                public class ManualModule : IModule
                {
                    public void ConfigureEndpoints(IEndpointRouteBuilder endpoints) { }
                }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var endpointExt = GetGeneratedSource(result, "EndpointExtensions.g.cs");

        endpointExt
            .Should()
            .Contain("ModuleExtensions.s_TestApp_ManualModule.ConfigureEndpoints(app);");
    }

    [Fact]
    public void Module_WithNoEndpoints_NothingEmittedForThatModule()
    {
        var source = """
            using SimpleModule.Core;

            namespace TestApp
            {
                [Module("Empty")]
                public class EmptyModule : IModule { }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var endpointExt = GetGeneratedSource(result, "EndpointExtensions.g.cs");

        endpointExt.Should().NotContain("EmptyModule");
        endpointExt.Should().NotContain("MapGroup");
    }

    [Fact]
    public void MultipleModules_WithDifferentPrefixes_EachGetOwnGroup()
    {
        var source = """
            using Microsoft.AspNetCore.Builder;
            using Microsoft.AspNetCore.Routing;
            using SimpleModule.Core;

            namespace ModuleA
            {
                [Module("Alpha", RoutePrefix = "/api/alpha")]
                public class AlphaModule : IModule { }
            }

            namespace ModuleA.Endpoints
            {
                public class AlphaEndpoint : IEndpoint
                {
                    public void Map(IEndpointRouteBuilder app)
                    {
                        app.MapGet("/", () => "alpha");
                    }
                }
            }

            namespace ModuleB
            {
                [Module("Beta", RoutePrefix = "/api/beta")]
                public class BetaModule : IModule { }
            }

            namespace ModuleB.Endpoints
            {
                public class BetaEndpoint : IEndpoint
                {
                    public void Map(IEndpointRouteBuilder app)
                    {
                        app.MapGet("/", () => "beta");
                    }
                }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var endpointExt = GetGeneratedSource(result, "EndpointExtensions.g.cs");

        endpointExt.Should().Contain("app.MapGroup(\"/api/alpha\").WithTags(\"Alpha\")");
        endpointExt.Should().Contain("app.MapGroup(\"/api/beta\").WithTags(\"Beta\")");
        endpointExt.Should().Contain("new global::ModuleA.Endpoints.AlphaEndpoint().Map(group)");
        endpointExt.Should().Contain("new global::ModuleB.Endpoints.BetaEndpoint().Map(group)");
    }

    [Fact]
    public void ViewEndpoints_WithViewPrefix_GetViewGroupWithExcludeFromDescription()
    {
        var source = """
            using Microsoft.AspNetCore.Builder;
            using Microsoft.AspNetCore.Routing;
            using SimpleModule.Core;

            namespace TestApp
            {
                [Module("Products", ViewPrefix = "/products")]
                public class ProductsModule : IModule { }
            }

            namespace TestApp.Views
            {
                public class BrowseEndpoint : IViewEndpoint
                {
                    public void Map(IEndpointRouteBuilder app)
                    {
                        app.MapGet("/browse", () => "browse");
                    }
                }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var endpointExt = GetGeneratedSource(result, "EndpointExtensions.g.cs");

        endpointExt
            .Should()
            .Contain(
                "var viewGroup = app.MapGroup(\"/products\").WithTags(\"Products\").ExcludeFromDescription();"
            );
        endpointExt.Should().Contain("new global::TestApp.Views.BrowseEndpoint().Map(viewGroup);");
    }

    [Fact]
    public void ViewEndpoints_WithoutViewPrefix_MapDirectlyToApp()
    {
        var source = """
            using Microsoft.AspNetCore.Builder;
            using Microsoft.AspNetCore.Routing;
            using SimpleModule.Core;

            namespace TestApp
            {
                [Module("Test")]
                public class TestModule : IModule { }
            }

            namespace TestApp.Views
            {
                public class HomeEndpoint : IViewEndpoint
                {
                    public void Map(IEndpointRouteBuilder app)
                    {
                        app.MapGet("/", () => "home");
                    }
                }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var endpointExt = GetGeneratedSource(result, "EndpointExtensions.g.cs");

        endpointExt.Should().Contain("new global::TestApp.Views.HomeEndpoint().Map(app);");
        endpointExt.Should().NotContain("viewGroup");
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
