using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SimpleModule.Generator.Tests.Helpers;

namespace SimpleModule.Generator.Tests;

public class IncrementalCachingTests
{
    [Fact]
    public void Generator_CachesDiscoveryData_OnIdenticalCompilation()
    {
        // Two-run pattern: first run populates the cache, second run should hit it.
        var source = """
            using SimpleModule.Core;
            using Microsoft.Extensions.DependencyInjection;
            using Microsoft.Extensions.Configuration;
            using Microsoft.AspNetCore.Routing;

            namespace TestApp;

            [Module("Test")]
            public class TestModule : IModule
            {
                public void ConfigureServices(IServiceCollection services, IConfiguration configuration) { }
                public void ConfigureEndpoints(IEndpointRouteBuilder endpoints) { }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var generator = new ModuleDiscovererGenerator();

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: new[] { generator.AsSourceGenerator() },
            driverOptions: new GeneratorDriverOptions(
                disabledOutputs: IncrementalGeneratorOutputKind.None,
                trackIncrementalGeneratorSteps: true
            )
        );

        // First run — populate cache.
        driver = driver.RunGenerators(compilation);

        // Second run with the same compilation — should hit cache.
        driver = driver.RunGenerators(compilation);
        var result = driver.GetRunResult().Results[0];

        // The RegisterSourceOutput step reuses prior output when its input (DiscoveryData) is equal.
        var outputs = result.TrackedOutputSteps.SelectMany(kvp => kvp.Value).ToList();
        outputs.Should().NotBeEmpty("source outputs should be tracked");
        outputs
            .Should()
            .OnlyContain(
                step =>
                    step.Outputs.All(o =>
                        o.Reason == IncrementalStepRunReason.Cached
                        || o.Reason == IncrementalStepRunReason.Unchanged
                    ),
                "second run with identical compilation must hit the cache"
            );
    }
}
