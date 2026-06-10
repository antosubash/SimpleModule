using FluentAssertions;
using Microsoft.CodeAnalysis;
using SimpleModule.Generator.Tests.Helpers;

namespace SimpleModule.Generator.Tests;

public class ModuleManifestEmitterTests
{
    private const string ModuleSource = """
        using Microsoft.AspNetCore.Builder;
        using Microsoft.AspNetCore.Http;
        using Microsoft.AspNetCore.Routing;
        using System.Threading.Tasks;
        using SimpleModule.Core;
        using SimpleModule.Core.Authorization;
        using SimpleModule.Core.Events;

        namespace TestApp
        {
            [Module("Flags", RoutePrefix = "/api/flags", ViewPrefix = "/flags", DisplayName = "Feature Flags")]
            public class FlagsModule : IModule { }

            public sealed class FlagsPermissions : IModulePermissions
            {
                public const string View = "Flags.View";
                public const string Manage = "Flags.Manage";
            }

            public sealed record FlagToggled(string Name) : DomainEvent;

            public sealed record ExternalThing(string Id) : DomainEvent;

            public class ExternalThingHandler
            {
                public Task Handle(ExternalThing evt) => Task.CompletedTask;
            }
        }

        namespace TestApp.Pages
        {
            public class ManageEndpoint : IViewEndpoint
            {
                public void Map(IEndpointRouteBuilder app)
                {
                    app.MapGet("/manage", () => Results.Ok());
                }
            }
        }
        """;

    private static readonly Dictionary<string, string> ModuleKindProperties = new()
    {
        ["build_property.SimpleModuleProjectKind"] = "Module",
    };

    [Fact]
    public void ModuleKind_EmitsManifestAttribute_WithExpectedFields()
    {
        var compilation = GeneratorTestHelper.CreateCompilation(ModuleSource);

        var result = GeneratorTestHelper.RunGenerator(compilation, ModuleKindProperties);

        var manifest = GetGeneratedSource(result, "ModuleManifest.g.cs");
        manifest.Should().Contain("assembly: global::SimpleModule.Core.Modules.ModuleManifest");
        manifest.Should().Contain("schemaVersion");
        manifest.Should().Contain("TestAssembly");
        manifest.Should().Contain("Flags");
        manifest.Should().Contain("Feature Flags");
        manifest.Should().Contain("/api/flags");
        manifest.Should().Contain("Flags.View");
        manifest.Should().Contain("Flags.Manage");
        manifest.Should().Contain("_content/TestAssembly/TestAssembly.pages.js");
        manifest.Should().Contain("TestApp.FlagToggled");
        manifest.Should().Contain("TestApp.ExternalThing");
    }

    [Fact]
    public void ModuleKind_DoesNotEmitHostArtifacts()
    {
        var compilation = GeneratorTestHelper.CreateCompilation(ModuleSource);

        var result = GeneratorTestHelper.RunGenerator(compilation, ModuleKindProperties);

        var fileNames = result.Results[0].GeneratedSources.Select(s => s.HintName).ToList();
        fileNames.Should().NotContain("ModuleExtensions.g.cs");
        fileNames.Should().NotContain("HostingExtensions.g.cs");
        fileNames.Should().NotContain("PageRegistry.g.cs");
    }

    [Fact]
    public void HostKind_DoesNotEmitManifest_AndKeepsClassicArtifacts()
    {
        var compilation = GeneratorTestHelper.CreateCompilation(ModuleSource);

        var result = GeneratorTestHelper.RunGenerator(compilation);

        var fileNames = result.Results[0].GeneratedSources.Select(s => s.HintName).ToList();
        fileNames.Should().NotContain("ModuleManifest.g.cs");
        fileNames.Should().Contain("ModuleExtensions.g.cs");
    }

    [Fact]
    public void FrameworkCompatOverride_IsEmittedVerbatim()
    {
        var compilation = GeneratorTestHelper.CreateCompilation(ModuleSource);

        var result = GeneratorTestHelper.RunGenerator(
            compilation,
            new Dictionary<string, string>
            {
                ["build_property.SimpleModuleProjectKind"] = "Module",
                ["build_property.SimpleModuleFrameworkCompat"] = ">=9.9.9 <10.0.0",
            }
        );

        var manifest = GetGeneratedSource(result, "ModuleManifest.g.cs");
        manifest.Should().Contain(">=9.9.9 <10.0.0");
    }

    private static string GetGeneratedSource(GeneratorDriverRunResult result, string hintName)
    {
        var source = result.Results[0].GeneratedSources.FirstOrDefault(s => s.HintName == hintName);
        source.SourceText.Should().NotBeNull($"expected generated source '{hintName}'");
        return source.SourceText.ToString();
    }
}
