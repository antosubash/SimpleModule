using FluentAssertions;
using SimpleModule.Generator.Tests.Helpers;

namespace SimpleModule.Generator.Tests;

public class ViewDiscoveryTests
{
    [Fact]
    public void EndpointInViewsNamespace_DiscoveredAsView_RoutedUnderViewPrefix()
    {
        var source = """
            using Microsoft.AspNetCore.Builder;
            using Microsoft.AspNetCore.Routing;
            using SimpleModule.Core;

            namespace TestApp
            {
                [Module("Test", ViewPrefix = "/test")]
                public class TestModule : IModule { }
            }

            namespace TestApp.Views
            {
                public class CreateEndpoint : IViewEndpoint
                {
                    public void Map(IEndpointRouteBuilder app)
                    {
                        app.MapGet("/create", () => "create");
                    }
                }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var endpointExt = result
            .GeneratedTrees.First(t =>
                t.FilePath.EndsWith("EndpointExtensions.g.cs", StringComparison.Ordinal)
            )
            .GetText()
            .ToString();

        endpointExt.Should().Contain("var viewGroup = app.MapGroup(\"/test\").WithTags(\"Test\").ExcludeFromDescription()");
        endpointExt.Should().Contain("new global::TestApp.Views.CreateEndpoint().Map(viewGroup)");
    }

    [Fact]
    public void EndpointInNonViewsNamespace_DiscoveredAsEndpoint_RoutedUnderRoutePrefix()
    {
        var source = """
            using Microsoft.AspNetCore.Builder;
            using Microsoft.AspNetCore.Routing;
            using SimpleModule.Core;

            namespace TestApp
            {
                [Module("Test", RoutePrefix = "/api/test")]
                public class TestModule : IModule { }
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

        var endpointExt = result
            .GeneratedTrees.First(t =>
                t.FilePath.EndsWith("EndpointExtensions.g.cs", StringComparison.Ordinal)
            )
            .GetText()
            .ToString();

        endpointExt.Should().Contain("var group = app.MapGroup(\"/api/test\")");
        endpointExt.Should().Contain("new global::TestApp.Endpoints.ListEndpoint().Map(group)");
        endpointExt.Should().NotContain("viewGroup");
    }

    [Fact]
    public void PageNameDerived_FromModuleNameAndClassName_StrippingEndpointSuffix()
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

        result
            .GeneratedTrees.Should()
            .Contain(t => t.FilePath.EndsWith("ViewPages_Products.g.cs", StringComparison.Ordinal));

        var viewPages = result
            .GeneratedTrees.First(t =>
                t.FilePath.EndsWith("ViewPages_Products.g.cs", StringComparison.Ordinal)
            )
            .GetText()
            .ToString();

        viewPages.Should().Contain("import Browse from '../Views/Browse'");
        viewPages.Should().Contain("'Products/Browse': Browse");
    }

    [Fact]
    public void ViewPages_GeneratesTypeScriptIndex()
    {
        var source = """
            using Microsoft.AspNetCore.Builder;
            using Microsoft.AspNetCore.Routing;
            using SimpleModule.Core;

            namespace TestApp
            {
                [Module("Test", ViewPrefix = "/test")]
                public class TestModule : IModule { }
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

                public class CreateEndpoint : IViewEndpoint
                {
                    public void Map(IEndpointRouteBuilder app)
                    {
                        app.MapGet("/create", () => "create");
                    }
                }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        result
            .GeneratedTrees.Should()
            .Contain(t => t.FilePath.EndsWith("ViewPages_Test.g.cs", StringComparison.Ordinal));

        var viewPages = result
            .GeneratedTrees.First(t =>
                t.FilePath.EndsWith("ViewPages_Test.g.cs", StringComparison.Ordinal)
            )
            .GetText()
            .ToString();

        viewPages.Should().Contain("import Browse from '../Views/Browse'");
        viewPages.Should().Contain("import Create from '../Views/Create'");
        viewPages.Should().Contain("'Test/Browse': Browse");
        viewPages.Should().Contain("'Test/Create': Create");
    }

    [Fact]
    public void ModuleWithViewsAndEndpoints_BothCoexist()
    {
        var source = """
            using Microsoft.AspNetCore.Builder;
            using Microsoft.AspNetCore.Routing;
            using SimpleModule.Core;

            namespace TestApp
            {
                [Module("Test", RoutePrefix = "/api/test", ViewPrefix = "/test")]
                public class TestModule : IModule { }
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

        var endpointExt = result
            .GeneratedTrees.First(t =>
                t.FilePath.EndsWith("EndpointExtensions.g.cs", StringComparison.Ordinal)
            )
            .GetText()
            .ToString();

        // Plain endpoints use RoutePrefix
        endpointExt.Should().Contain("var group = app.MapGroup(\"/api/test\")");
        endpointExt.Should().Contain("new global::TestApp.Endpoints.ListEndpoint().Map(group)");

        // Views use ViewPrefix
        endpointExt.Should().Contain("var viewGroup = app.MapGroup(\"/test\").WithTags(\"Test\").ExcludeFromDescription()");
        endpointExt.Should().Contain("new global::TestApp.Views.BrowseEndpoint().Map(viewGroup)");
    }

    [Fact]
    public void ViewClassName_WithViewSuffix_StrippedCorrectly()
    {
        var source = """
            using Microsoft.AspNetCore.Builder;
            using Microsoft.AspNetCore.Routing;
            using SimpleModule.Core;

            namespace TestApp
            {
                [Module("Test", ViewPrefix = "/test")]
                public class TestModule : IModule { }
            }

            namespace TestApp.Views
            {
                public class DetailView : IViewEndpoint
                {
                    public void Map(IEndpointRouteBuilder app)
                    {
                        app.MapGet("/{id}", () => "detail");
                    }
                }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var viewPages = result
            .GeneratedTrees.First(t =>
                t.FilePath.EndsWith("ViewPages_Test.g.cs", StringComparison.Ordinal)
            )
            .GetText()
            .ToString();

        viewPages.Should().Contain("import Detail from '../Views/Detail'");
        viewPages.Should().Contain("'Test/Detail': Detail");
    }

    [Fact]
    public void IEndpointInViewsNamespace_DiscoveredAsEndpoint_NotView()
    {
        var source = """
            using Microsoft.AspNetCore.Builder;
            using Microsoft.AspNetCore.Routing;
            using SimpleModule.Core;

            namespace TestApp
            {
                [Module("Test", RoutePrefix = "/api/test", ViewPrefix = "/test")]
                public class TestModule : IModule { }
            }

            namespace TestApp.Views
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

        var endpointExt = result
            .GeneratedTrees.First(t =>
                t.FilePath.EndsWith("EndpointExtensions.g.cs", StringComparison.Ordinal)
            )
            .GetText()
            .ToString();

        endpointExt.Should().Contain("new global::TestApp.Views.ListEndpoint().Map(group)");
        endpointExt.Should().NotContain("viewGroup");
    }
}
