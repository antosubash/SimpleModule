using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace SimpleModule.Generator;

internal static class PolicyChecks
{
    internal static void Run(SourceProductionContext context, DiscoveryData data)
    {
        if (data.Policies.Length == 0)
            return;

        // DtoTypes already contains both [Dto]-attributed types and convention DTOs
        // (public types in .Contracts assemblies), so membership here is the full
        // definition of "contracts DTO".
        var dtoFqns = new HashSet<string>();
        foreach (var dto in data.DtoTypes)
            dtoFqns.Add(dto.FullyQualifiedName);

        foreach (var policy in data.Policies)
        {
            if (dtoFqns.Contains(policy.ResourceTypeFqn))
                continue;

            context.ReportDiagnostic(
                Diagnostic.Create(
                    DiagnosticDescriptors.PolicyResourceMustBeDto,
                    LocationHelper.ToLocation(policy.Location),
                    TypeMappingHelpers.StripGlobalPrefix(policy.FullyQualifiedName),
                    TypeMappingHelpers.StripGlobalPrefix(policy.ResourceTypeFqn)
                )
            );
        }
    }
}
