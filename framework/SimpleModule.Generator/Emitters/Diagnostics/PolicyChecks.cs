using Microsoft.CodeAnalysis;

namespace SimpleModule.Generator;

internal static class PolicyChecks
{
    internal static void Run(SourceProductionContext context, DiscoveryData data)
    {
        foreach (var policy in data.Policies)
        {
            var policyName = TypeMappingHelpers.StripGlobalPrefix(policy.FullyQualifiedName);
            var resourceName = TypeMappingHelpers.StripGlobalPrefix(policy.ResourceTypeFqn);

            // SM0058: resource must be a contracts DTO ([Dto] or in a .Contracts
            // assembly). Determined symbolically at discovery so contracts entities
            // excluded from DtoTypes ([NoDtoGeneration], IEvent) still qualify.
            if (!policy.ResourceIsContractsDto)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.PolicyResourceMustBeDto,
                        LocationHelper.ToLocation(policy.Location),
                        policyName,
                        resourceName
                    )
                );
            }

            // SM0059: non-public policies are skipped by the registration emitter,
            // which would otherwise surface only as a runtime MissingPolicyException.
            if (!policy.IsPublic)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.PolicyNotPublic,
                        LocationHelper.ToLocation(policy.Location),
                        policyName,
                        resourceName
                    )
                );
            }

            // SM0060: deny-wins lets any registered policy veto a decision, so a
            // policy for another module's resource is a cross-module backdoor.
            if (
                policy.ResourceModuleName.Length > 0
                && policy.ModuleName.Length > 0
                && policy.ResourceModuleName != policy.ModuleName
            )
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.PolicyForForeignResource,
                        LocationHelper.ToLocation(policy.Location),
                        policyName,
                        policy.ModuleName,
                        resourceName,
                        policy.ResourceModuleName
                    )
                );
            }
        }
    }
}
