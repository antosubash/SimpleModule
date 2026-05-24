using FluentAssertions;
using SimpleModule.Generator.Tests.Helpers;

namespace SimpleModule.Generator.Tests;

/// <summary>
/// Tests for SM0056 (FormRequest must be sealed) and SM0057 (FormRequest must extend FormRequest&lt;TSelf&gt;).
/// The generator only runs FormRequestChecks when at least one [Module] exists in the compilation
/// (SymbolDiscovery returns DiscoveryData.Empty when no modules are found), so every test source
/// includes a minimal module declaration.
/// </summary>
public class FormRequestDiagnosticTests
{
    #region SM0056: FormRequest class must be sealed

    [Fact]
    public void SM0056_NonSealedFormRequest_ReportsError()
    {
        var source = """
            using SimpleModule.Core;
            using SimpleModule.Core.FormRequests;

            namespace TestApp
            {
                [Module("Products")]
                public class ProductsModule : IModule { }
            }

            namespace TestApp.FormRequests
            {
                [FormRequest]
                public class OpenFormRequest : FormRequest<OpenFormRequest>
                {
                    public string Name { get; set; } = "";

                    protected override void ConfigureRules(RuleConfigurator<OpenFormRequest> rules)
                    {
                        rules.RuleFor(x => x.Name).NotEmpty();
                    }
                }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilationWithFormRequestSupport(source);
        var (_, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(compilation);

        diagnostics.Should().Contain(d => d.Id == "SM0056");
        var diag = diagnostics.First(d => d.Id == "SM0056");
        diag.GetMessage(System.Globalization.CultureInfo.InvariantCulture)
            .Should()
            .Contain("OpenFormRequest");
    }

    [Fact]
    public void SM0056_SealedFormRequest_NoDiagnostic()
    {
        var source = """
            using SimpleModule.Core;
            using SimpleModule.Core.FormRequests;

            namespace TestApp
            {
                [Module("Products")]
                public class ProductsModule : IModule { }
            }

            namespace TestApp.FormRequests
            {
                [FormRequest]
                public sealed class SealedFormRequest : FormRequest<SealedFormRequest>
                {
                    public string Name { get; set; } = "";

                    protected override void ConfigureRules(RuleConfigurator<SealedFormRequest> rules)
                    {
                        rules.RuleFor(x => x.Name).NotEmpty();
                    }
                }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilationWithFormRequestSupport(source);
        var (_, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(compilation);

        diagnostics.Should().NotContain(d => d.Id == "SM0056");
    }

    [Fact]
    public void SM0056_MultipleNonSealedFormRequests_ReportsErrorForEach()
    {
        var source = """
            using SimpleModule.Core;
            using SimpleModule.Core.FormRequests;

            namespace TestApp
            {
                [Module("Orders")]
                public class OrdersModule : IModule { }
            }

            namespace TestApp.FormRequests
            {
                [FormRequest]
                public class FirstRequest : FormRequest<FirstRequest>
                {
                    protected override void ConfigureRules(RuleConfigurator<FirstRequest> rules) { }
                }

                [FormRequest]
                public class SecondRequest : FormRequest<SecondRequest>
                {
                    protected override void ConfigureRules(RuleConfigurator<SecondRequest> rules) { }
                }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilationWithFormRequestSupport(source);
        var (_, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(compilation);

        diagnostics.Where(d => d.Id == "SM0056").Should().HaveCount(2);
    }

    #endregion

    #region SM0057: FormRequest class must extend FormRequest<TSelf>

    [Fact]
    public void SM0057_FormRequestWithoutBase_ReportsError()
    {
        var source = """
            using SimpleModule.Core;
            using SimpleModule.Core.FormRequests;

            namespace TestApp
            {
                [Module("Products")]
                public class ProductsModule : IModule { }
            }

            namespace TestApp.FormRequests
            {
                [FormRequest]
                public sealed class BadFormRequest
                {
                    public string Name { get; set; } = "";
                }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilationWithFormRequestSupport(source);
        var (_, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(compilation);

        diagnostics.Should().Contain(d => d.Id == "SM0057");
        var diag = diagnostics.First(d => d.Id == "SM0057");
        diag.GetMessage(System.Globalization.CultureInfo.InvariantCulture)
            .Should()
            .Contain("BadFormRequest");
    }

    [Fact]
    public void SM0057_FormRequestWithWrongBase_ReportsError()
    {
        // Has [FormRequest] but extends an arbitrary class, not FormRequest<TSelf>
        var source = """
            using SimpleModule.Core;
            using SimpleModule.Core.FormRequests;

            namespace TestApp
            {
                [Module("Products")]
                public class ProductsModule : IModule { }
            }

            namespace TestApp.FormRequests
            {
                public abstract class SomeBase { }

                [FormRequest]
                public sealed class WrongBaseRequest : SomeBase
                {
                    public string Name { get; set; } = "";
                }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilationWithFormRequestSupport(source);
        var (_, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(compilation);

        diagnostics.Should().Contain(d => d.Id == "SM0057");
    }

    [Fact]
    public void SM0057_ProperFormRequest_NoDiagnostic()
    {
        var source = """
            using SimpleModule.Core;
            using SimpleModule.Core.FormRequests;

            namespace TestApp
            {
                [Module("Products")]
                public class ProductsModule : IModule { }
            }

            namespace TestApp.FormRequests
            {
                [FormRequest]
                public sealed class GoodRequest : FormRequest<GoodRequest>
                {
                    public string Value { get; set; } = "";

                    protected override void ConfigureRules(RuleConfigurator<GoodRequest> rules)
                    {
                        rules.RuleFor(x => x.Value).NotEmpty();
                    }
                }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilationWithFormRequestSupport(source);
        var (_, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(compilation);

        diagnostics.Should().NotContain(d => d.Id == "SM0057");
    }

    [Fact]
    public void SM0056_And_SM0057_BothFire_WhenClassHasAttributeButNoBaseAndIsNotSealed()
    {
        // A class with [FormRequest] that is neither sealed nor extending the base class should
        // produce both SM0056 and SM0057 simultaneously.
        var source = """
            using SimpleModule.Core;
            using SimpleModule.Core.FormRequests;

            namespace TestApp
            {
                [Module("Products")]
                public class ProductsModule : IModule { }
            }

            namespace TestApp.FormRequests
            {
                [FormRequest]
                public class DoublyBadRequest
                {
                    public string Name { get; set; } = "";
                }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilationWithFormRequestSupport(source);
        var (_, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(compilation);

        diagnostics.Should().Contain(d => d.Id == "SM0056");
        diagnostics.Should().Contain(d => d.Id == "SM0057");
    }

    #endregion

    #region No false positives on valid FormRequest types

    [Fact]
    public void ValidFormRequest_ProducesNoFormRequestDiagnostics()
    {
        var source = """
            using SimpleModule.Core;
            using SimpleModule.Core.FormRequests;

            namespace TestApp
            {
                [Module("Catalog")]
                public class CatalogModule : IModule { }
            }

            namespace TestApp.FormRequests
            {
                [FormRequest]
                public sealed class CreateProductRequest : FormRequest<CreateProductRequest>
                {
                    public string Name { get; set; } = "";
                    public decimal Price { get; set; }

                    protected override void ConfigureRules(RuleConfigurator<CreateProductRequest> rules)
                    {
                        rules.RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
                        rules.RuleFor(x => x.Price).GreaterThan(0);
                    }
                }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilationWithFormRequestSupport(source);
        var (_, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(compilation);

        diagnostics
            .Where(d => d.Id == "SM0056" || d.Id == "SM0057")
            .Should()
            .BeEmpty("a correctly-defined FormRequest should produce no SM0056/SM0057 diagnostics");
    }

    [Fact]
    public void TypeWithoutFormRequestAttribute_NotSealed_NoDiagnostic()
    {
        // A class that extends FormRequest<T> but lacks the [FormRequest] attribute
        // should not be reported — the attribute is the trigger for the finder.
        var source = """
            using SimpleModule.Core;
            using SimpleModule.Core.FormRequests;

            namespace TestApp
            {
                [Module("Products")]
                public class ProductsModule : IModule { }
            }

            namespace TestApp.FormRequests
            {
                // No [FormRequest] attribute — not subject to SM0056/SM0057 checks.
                public class UnmarkedRequest : FormRequest<UnmarkedRequest>
                {
                    public string Name { get; set; } = "";

                    protected override void ConfigureRules(RuleConfigurator<UnmarkedRequest> rules)
                    {
                        rules.RuleFor(x => x.Name).NotEmpty();
                    }
                }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilationWithFormRequestSupport(source);
        var (_, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(compilation);

        diagnostics.Should().NotContain(d => d.Id == "SM0056");
        diagnostics.Should().NotContain(d => d.Id == "SM0057");
    }

    #endregion

    #region Generator emits AddFormRequestFilter on route groups

    [Fact]
    public void Module_WithRoutePrefix_GeneratesAddFormRequestFilterOnGroup()
    {
        var source = """
            using Microsoft.AspNetCore.Builder;
            using Microsoft.AspNetCore.Routing;
            using SimpleModule.Core;

            namespace TestApp
            {
                [Module("Orders", RoutePrefix = "/api/orders")]
                public class OrdersModule : IModule { }
            }

            namespace TestApp.Endpoints
            {
                public class ListOrdersEndpoint : IEndpoint
                {
                    public void Map(IEndpointRouteBuilder app)
                    {
                        app.MapGet("/", () => "orders");
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

        // The filter must be applied on every route group so FormRequest validation fires automatically.
        endpointExt.Should().Contain(".AddFormRequestFilter()");
    }

    [Fact]
    public void Module_WithoutRoutePrefix_StillGeneratesAddFormRequestFilter()
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

        var endpointExt = result
            .GeneratedTrees.First(t =>
                t.FilePath.EndsWith("EndpointExtensions.g.cs", StringComparison.Ordinal)
            )
            .GetText()
            .ToString();

        // Even without RoutePrefix, a group is created with the FormRequest filter
        endpointExt.Should().Contain(".AddFormRequestFilter()");
    }

    #endregion
}
