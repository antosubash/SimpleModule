using FluentAssertions;
using SimpleModule.Cli.Infrastructure;

namespace SimpleModule.Cli.Tests;

public sealed class BundleExternalsValidatorTests : IDisposable
{
    private readonly string _wwwroot = Path.Combine(
        Path.GetTempPath(),
        "sm-externals-tests-" + Guid.NewGuid().ToString("N")
    );

    public BundleExternalsValidatorTests() => Directory.CreateDirectory(_wwwroot);

    [Fact]
    public void ExternalizedBundle_Passes()
    {
        Write(
            "Module.pages.js",
            """
            import { jsx } from "react/jsx-runtime";
            import { usePage } from "@inertiajs/react";
            var pages = { "X/Browse": () => import("./Browse-abc.mjs") };
            export { pages };
            """
        );
        Write(
            "Browse-abc.mjs",
            """
            import * as React from "react";
            export default function Browse() { return React.createElement("div"); }
            """
        );

        var violations = BundleExternalsValidator.Validate(_wwwroot);

        violations.Should().BeEmpty();
    }

    [Fact]
    public void InlinedReact_FailsWithFileAndMarker()
    {
        Write(
            "Module.pages.js",
            """
            var ReactSymbol = Symbol.for("react.element");
            function jsxProd(type, config) { return { $$typeof: ReactSymbol }; }
            """
        );

        var violations = BundleExternalsValidator.Validate(_wwwroot);

        violations.Should().ContainSingle();
        violations[0].File.Should().Contain("Module.pages.js");
        violations[0].Marker.Should().Contain("react.element");
    }

    [Theory]
    [InlineData("""var x = "react.production.min";""")]
    [InlineData(
        """var internals = __CLIENT_INTERNALS_DO_NOT_USE_OR_WARN_USERS_THEY_CANNOT_UPGRADE;"""
    )]
    [InlineData("""var s = Symbol.for("react.transitional.element");""")]
    public void OtherInlineMarkers_Fail(string content)
    {
        Write("chunk-x.mjs", content);

        BundleExternalsValidator.Validate(_wwwroot).Should().NotBeEmpty();
    }

    [Fact]
    public void EmptyDirectory_PassesWithNoViolations()
    {
        BundleExternalsValidator.Validate(_wwwroot).Should().BeEmpty();
    }

    [Fact]
    public void SourceMaps_AreIgnored()
    {
        Write(
            "Module.pages.js.map",
            """{"mappings": "react.element Symbol.for(\"react.element\")"}"""
        );

        BundleExternalsValidator.Validate(_wwwroot).Should().BeEmpty();
    }

    private void Write(string name, string content) =>
        File.WriteAllText(Path.Combine(_wwwroot, name), content);

    public void Dispose()
    {
        try
        {
            Directory.Delete(_wwwroot, recursive: true);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
