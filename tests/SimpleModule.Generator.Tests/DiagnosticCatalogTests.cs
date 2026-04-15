using System.Collections.Generic;
using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis;

namespace SimpleModule.Generator.Tests;

public class DiagnosticCatalogTests
{
    // Baseline captured from DiagnosticDescriptors.cs on the post-refactor commit.
    // If you intentionally add/remove a diagnostic, update this table in the same commit.
    // The test uses this to catch accidental drift of SM00xx IDs, severities, or categories.
    private static readonly Dictionary<
        string,
        (string Id, DiagnosticSeverity Severity, string Category)
    > Expected = new()
    {
        ["DuplicateDbSetPropertyName"] = (
            "SM0001",
            DiagnosticSeverity.Error,
            "SimpleModule.Generator"
        ),
        ["EmptyModuleName"] = ("SM0002", DiagnosticSeverity.Warning, "SimpleModule.Generator"),
        ["MultipleIdentityDbContexts"] = (
            "SM0003",
            DiagnosticSeverity.Error,
            "SimpleModule.Generator"
        ),
        ["IdentityDbContextBadTypeArgs"] = (
            "SM0005",
            DiagnosticSeverity.Error,
            "SimpleModule.Generator"
        ),
        ["EntityConfigForMissingEntity"] = (
            "SM0006",
            DiagnosticSeverity.Warning,
            "SimpleModule.Generator"
        ),
        ["DuplicateEntityConfiguration"] = (
            "SM0007",
            DiagnosticSeverity.Error,
            "SimpleModule.Generator"
        ),
        ["CircularModuleDependency"] = (
            "SM0010",
            DiagnosticSeverity.Error,
            "SimpleModule.Generator"
        ),
        ["IllegalImplementationReference"] = (
            "SM0011",
            DiagnosticSeverity.Error,
            "SimpleModule.Generator"
        ),
        ["ContractInterfaceTooLargeWarning"] = (
            "SM0012",
            DiagnosticSeverity.Warning,
            "SimpleModule.Generator"
        ),
        ["ContractInterfaceTooLargeError"] = (
            "SM0013",
            DiagnosticSeverity.Error,
            "SimpleModule.Generator"
        ),
        ["MissingContractInterfaces"] = (
            "SM0014",
            DiagnosticSeverity.Error,
            "SimpleModule.Generator"
        ),
        ["DuplicateViewPageName"] = ("SM0015", DiagnosticSeverity.Error, "SimpleModule.Generator"),
        ["NoContractImplementation"] = (
            "SM0025",
            DiagnosticSeverity.Error,
            "SimpleModule.Generator"
        ),
        ["MultipleContractImplementations"] = (
            "SM0026",
            DiagnosticSeverity.Error,
            "SimpleModule.Generator"
        ),
        ["PermissionFieldNotConstString"] = (
            "SM0027",
            DiagnosticSeverity.Error,
            "SimpleModule.Generator"
        ),
        ["ContractImplementationNotPublic"] = (
            "SM0028",
            DiagnosticSeverity.Error,
            "SimpleModule.Generator"
        ),
        ["ContractImplementationIsAbstract"] = (
            "SM0029",
            DiagnosticSeverity.Error,
            "SimpleModule.Generator"
        ),
        ["PermissionValueBadPattern"] = (
            "SM0031",
            DiagnosticSeverity.Warning,
            "SimpleModule.Generator"
        ),
        ["PermissionClassNotSealed"] = (
            "SM0032",
            DiagnosticSeverity.Error,
            "SimpleModule.Generator"
        ),
        ["DuplicatePermissionValue"] = (
            "SM0033",
            DiagnosticSeverity.Error,
            "SimpleModule.Generator"
        ),
        ["PermissionValueWrongPrefix"] = (
            "SM0034",
            DiagnosticSeverity.Warning,
            "SimpleModule.Generator"
        ),
        ["DtoTypeNoProperties"] = ("SM0035", DiagnosticSeverity.Warning, "SimpleModule.Generator"),
        ["InfrastructureTypeInContracts"] = (
            "SM0038",
            DiagnosticSeverity.Warning,
            "SimpleModule.Generator"
        ),
        ["InterceptorDependsOnDbContext"] = (
            "SM0039",
            DiagnosticSeverity.Warning,
            "SimpleModule.Generator"
        ),
        ["DuplicateModuleName"] = ("SM0040", DiagnosticSeverity.Error, "SimpleModule.Generator"),
        ["ViewPagePrefixMismatch"] = (
            "SM0041",
            DiagnosticSeverity.Warning,
            "SimpleModule.Generator"
        ),
        ["ViewEndpointWithoutViewPrefix"] = (
            "SM0042",
            DiagnosticSeverity.Error,
            "SimpleModule.Generator"
        ),
        ["EmptyModuleWarning"] = ("SM0043", DiagnosticSeverity.Warning, "SimpleModule.Generator"),
        ["MultipleModuleOptions"] = (
            "SM0044",
            DiagnosticSeverity.Warning,
            "SimpleModule.Generator"
        ),
        ["FeatureClassNotSealed"] = ("SM0045", DiagnosticSeverity.Error, "SimpleModule.Generator"),
        ["FeatureFieldNamingViolation"] = (
            "SM0046",
            DiagnosticSeverity.Warning,
            "SimpleModule.Generator"
        ),
        ["DuplicateFeatureName"] = ("SM0047", DiagnosticSeverity.Error, "SimpleModule.Generator"),
        ["FeatureFieldNotConstString"] = (
            "SM0048",
            DiagnosticSeverity.Error,
            "SimpleModule.Generator"
        ),
        ["MultipleEndpointsPerFile"] = (
            "SM0049",
            DiagnosticSeverity.Error,
            "SimpleModule.Generator"
        ),
        ["ModuleAssemblyNamingViolation"] = (
            "SM0052",
            DiagnosticSeverity.Error,
            "SimpleModule.Generator"
        ),
        ["MissingContractsAssembly"] = (
            "SM0053",
            DiagnosticSeverity.Error,
            "SimpleModule.Generator"
        ),
        ["MissingEndpointRouteConst"] = (
            "SM0054",
            DiagnosticSeverity.Info,
            "SimpleModule.Generator"
        ),
        ["EntityNotInContractsAssembly"] = (
            "SM0055",
            DiagnosticSeverity.Error,
            "SimpleModule.Generator"
        ),
    };

    [Fact]
    public void AllDescriptorsMatchBaseline()
    {
        var descriptorsType = typeof(ModuleDiscovererGenerator).Assembly.GetType(
            "SimpleModule.Generator.DiagnosticDescriptors"
        );
        descriptorsType
            .Should()
            .NotBeNull("DiagnosticDescriptors class must exist in the generator assembly");

        var actual =
            new Dictionary<string, (string Id, DiagnosticSeverity Severity, string Category)>();
        foreach (
            var field in descriptorsType!.GetFields(
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public
            )
        )
        {
            if (field.GetValue(null) is DiagnosticDescriptor d)
                actual[field.Name] = (d.Id, d.DefaultSeverity, d.Category);
        }

        actual.Should().HaveCount(Expected.Count, "the set of descriptors must match the baseline");

        foreach (var kvp in Expected)
        {
            actual.Should().ContainKey(kvp.Key);
            actual[kvp.Key]
                .Should()
                .Be(kvp.Value, $"descriptor {kvp.Key} should match the baseline");
        }
    }
}
