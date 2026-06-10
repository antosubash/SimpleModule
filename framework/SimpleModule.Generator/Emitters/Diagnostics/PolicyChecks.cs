using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace SimpleModule.Generator;

internal static class PolicyChecks
{
    internal static void Run(SourceProductionContext context, DiscoveryData data)
    {
        // SM0059/SM0061 are class-level rules but discovery yields one record per
        // implemented IPolicy<T> interface — dedup so a multi-resource policy class
        // reports each class-level diagnostic once.
        var reportedNonPublic = new HashSet<string>();
        var reportedGeneric = new HashSet<string>();

        foreach (var policy in data.Policies)
        {
            var policyName = TypeMappingHelpers.StripGlobalPrefix(policy.FullyQualifiedName);
            var resourceName = TypeMappingHelpers.StripGlobalPrefix(policy.ResourceTypeFqn);

            // SM0059/SM0061 exist to catch policies the generator cannot register; a
            // [ManualContractRegistration] policy is wired by its own module (which can
            // reference internal types and close generics), so both rules are waived.
            // SM0058/SM0060 are resource rules and still apply below.
            var autoRegistered = !policy.IsManuallyRegistered;

            // SM0061: open generic policies cannot be registered; the resource type is
            // a type parameter, so the remaining checks would only add noise.
            if (policy.IsGeneric)
            {
                if (!autoRegistered)
                    continue;

                if (reportedGeneric.Add(policy.FullyQualifiedName))
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            DiagnosticDescriptors.PolicyMustNotBeGeneric,
                            LocationHelper.ToLocation(policy.Location),
                            policyName
                        )
                    );
                }
                continue;
            }

            // SM0058: resource must be an effectively-public contracts DTO ([Dto] or
            // in a .Contracts assembly). Determined symbolically at discovery so
            // contracts entities excluded from DtoTypes ([NoDtoGeneration], IEvent)
            // still qualify.
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
            if (
                autoRegistered
                && !policy.IsPublic
                && reportedNonPublic.Add(policy.FullyQualifiedName)
            )
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
