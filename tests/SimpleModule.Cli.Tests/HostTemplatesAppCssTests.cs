using FluentAssertions;
using SimpleModule.Cli.Templates;

namespace SimpleModule.Cli.Tests;

public sealed class HostTemplatesAppCssTests
{
    [Fact]
    public void AppCss_KeepsScanSource_SoPackagedModuleClassesAreCompiled()
    {
        // SimpleModule.Hosting.targets stages every module's built .pages.js into
        // Styles/_scan/, and that is the only way Tailwind sees classes from a
        // packaged module or from components living outside Pages/. The path is
        // already relative to Styles/, so it needs no rewriting for the scaffold —
        // dropping it left scaffolded apps missing those utility classes (#290).
        HostTemplates.AppCss().Should().Contain("@source \"./_scan/\";");
    }

    [Fact]
    public void AppCss_RewritesFrameworkPackageRoots_ToNodeModules()
    {
        var css = HostTemplates.AppCss();

        css.Should().Contain("../../../node_modules/@simplemodule/theme-default/theme.css");
        css.Should().Contain("../../../node_modules/@simplemodule/ui/");
        css.Should().Contain("../../../node_modules/@simplemodule/client/");
        // A scaffold has no packages/ directory, so no in-repo root may survive.
        css.Should().NotContain("../../../packages/");
    }

    [Fact]
    public void AppCss_RewritesModuleRoots_ToScaffoldDepth()
    {
        var css = HostTemplates.AppCss();

        // Modules sit at src/modules/ in a scaffold: two ups from Styles/, not three.
        css.Should().Contain("@source \"../../modules/**/Pages/**/*.tsx\";");
        css.Should().NotContain("../../../modules/");
    }
}
