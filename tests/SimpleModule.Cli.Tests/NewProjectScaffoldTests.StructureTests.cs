using FluentAssertions;
using SimpleModule.Cli.Commands.New;
using SimpleModule.Cli.Infrastructure;

namespace SimpleModule.Cli.Tests;

public sealed partial class NewProjectScaffoldTests
{
    [Fact]
    public void Scaffold_CreatesExpectedFiles()
    {
        var (projectName, rootDir) = ScaffoldStandalone();

        File.Exists(Path.Combine(rootDir, $"{projectName}.slnx")).Should().BeTrue();
        File.Exists(Path.Combine(rootDir, "Directory.Build.props")).Should().BeTrue();
        File.Exists(Path.Combine(rootDir, "Directory.Packages.props")).Should().BeTrue();
        File.Exists(Path.Combine(rootDir, "nuget.config")).Should().BeTrue();
        File.Exists(Path.Combine(rootDir, "global.json")).Should().BeTrue();
        File.Exists(
                Path.Combine(rootDir, "src", $"{projectName}.Host", $"{projectName}.Host.csproj")
            )
            .Should()
            .BeTrue();
        File.Exists(Path.Combine(rootDir, "src", $"{projectName}.Host", "Program.cs"))
            .Should()
            .BeTrue();
        File.Exists(
                Path.Combine(rootDir, "src", "modules", "Items", "src", "Items", "Items.csproj")
            )
            .Should()
            .BeTrue();
        File.Exists(
                Path.Combine(
                    rootDir,
                    "src",
                    "modules",
                    "Items",
                    "src",
                    "Items.Contracts",
                    "Items.Contracts.csproj"
                )
            )
            .Should()
            .BeTrue();
    }

    [Fact]
    public void Scaffold_DirectoryPackagesProps_UsesPublishedVersion()
    {
        var (_, rootDir) = ScaffoldStandalone();

        var content = File.ReadAllText(Path.Combine(rootDir, "Directory.Packages.props"));
        content.Should().Contain($"Version=\"{TestVersion}\"");
        content.Should().NotContain("0.1.0-local");
    }

    [Fact]
    public void Scaffold_NugetConfig_OnlyContainsNuGetOrg()
    {
        var (_, rootDir) = ScaffoldStandalone();

        var content = File.ReadAllText(Path.Combine(rootDir, "nuget.config"));
        content.Should().Contain("nuget.org");
        content.Should().NotContain("SimpleModule-Local");
        content.Should().NotContain("nupkg");
    }

    [Fact]
    public void Scaffold_PackageJson_UsesPublishedNpmPackages()
    {
        var (_, rootDir) = ScaffoldStandalone();

        var content = File.ReadAllText(Path.Combine(rootDir, "package.json"));
        content.Should().Contain($"\"@simplemodule/client\": \"^{TestVersion}\"");
        content.Should().Contain($"\"@simplemodule/ui\": \"^{TestVersion}\"");
        content.Should().Contain($"\"@simplemodule/theme-default\": \"^{TestVersion}\"");
        content.Should().NotContain("file:");
    }

    [Fact]
    public void Scaffold_WithSolution_UsesLocalPackages()
    {
        var solution = SolutionContext.Discover();
        if (solution is null)
        {
            return;
        }

        const string projectName = "TestApp";
        var rootDir = Path.Combine(_tempDir, projectName);

        NewProjectCommand.ScaffoldProject(
            projectName,
            rootDir,
            solution,
            frameworkVersion: TestVersion
        );

        var content = File.ReadAllText(Path.Combine(rootDir, "package.json"));
        content.Should().Contain("file:");
    }

    [Fact]
    public void Scaffold_ShipsFaviconSvg()
    {
        // Issue #225: index.html links /favicon.svg and Program.cs maps /favicon.ico to it,
        // so the scaffold must ship the file (otherwise both 404 on every page).
        var (projectName, rootDir) = ScaffoldStandalone();

        var favicon = Path.Combine(rootDir, "src", $"{projectName}.Host", "wwwroot", "favicon.svg");
        File.Exists(favicon).Should().BeTrue();
        File.ReadAllText(favicon).Should().Contain("<svg");
    }

    [Fact]
    public void Scaffold_ModuleAndContractsUseBareAssemblyNames()
    {
        // Issue #228: the module (RCL) AssemblyName must equal the directory basename so the
        // Vite pages bundle (named from the basename) serves at /_content/{AssemblyName}/ and
        // resolves. The contracts AssemblyName must stay paired (module + ".Contracts") so the
        // source generator discovers the module's contract implementations.
        var (_, rootDir) = ScaffoldStandalone();

        var moduleCsproj = File.ReadAllText(
            Path.Combine(rootDir, "src", "modules", "Items", "src", "Items", "Items.csproj")
        );
        moduleCsproj.Should().Contain("<AssemblyName>Items</AssemblyName>");
        moduleCsproj.Should().NotContain("<AssemblyName>SimpleModule.Items</AssemblyName>");

        var contractsCsproj = File.ReadAllText(
            Path.Combine(
                rootDir,
                "src",
                "modules",
                "Items",
                "src",
                "Items.Contracts",
                "Items.Contracts.csproj"
            )
        );
        contractsCsproj.Should().Contain("<AssemblyName>Items.Contracts</AssemblyName>");
    }

    [Fact]
    public void Scaffold_EventRecordDerivesFromDomainEvent()
    {
        // Issue #218: IEvent requires EventId/OccurredAt, so generated event records must derive
        // from DomainEvent (which supplies them) rather than implementing IEvent directly.
        var (_, rootDir) = ScaffoldStandalone();

        var eventFile = File.ReadAllText(
            Path.Combine(
                rootDir,
                "src",
                "modules",
                "Items",
                "src",
                "Items.Contracts",
                "Events",
                "ItemCreatedEvent.cs"
            )
        );
        eventFile.Should().Contain(": DomainEvent");
        eventFile.Should().NotContain(": IEvent");
    }

    [Fact]
    public void RootPackageJson_PinsNpmDependenciesToNpmVersion_NotFrameworkVersion()
    {
        // Issue #219: the @simplemodule/* npm packages are not always published in lockstep with
        // NuGet, so the scaffold must pin them to the resolved npm version independently.
        var templates = new Templates.ProjectTemplates(
            solution: null,
            frameworkVersion: "0.0.40",
            npmVersion: "0.0.36"
        );

        var packageJson = templates.RootPackageJson("TestApp", frameworkPackagesPath: null);

        packageJson.Should().Contain("\"@simplemodule/client\": \"^0.0.36\"");
        packageJson.Should().Contain("\"@simplemodule/ui\": \"^0.0.36\"");
        packageJson.Should().Contain("\"@simplemodule/theme-default\": \"^0.0.36\"");
        packageJson.Should().NotContain("0.0.40");
    }
}
