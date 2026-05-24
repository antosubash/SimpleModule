using Microsoft.CodeAnalysis;

namespace SimpleModule.Generator;

internal static class FormRequestChecks
{
    internal static void Run(SourceProductionContext context, DiscoveryData data)
    {
        foreach (var formRequest in data.FormRequests)
        {
            if (!formRequest.IsSealed)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.FormRequestNotSealed,
                        LocationHelper.ToLocation(formRequest.Location),
                        TypeMappingHelpers.StripGlobalPrefix(formRequest.FullyQualifiedName)
                    )
                );
            }

            if (!formRequest.ExtendsFormRequest)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.FormRequestDoesNotExtendBase,
                        LocationHelper.ToLocation(formRequest.Location),
                        TypeMappingHelpers.StripGlobalPrefix(formRequest.FullyQualifiedName)
                    )
                );
            }
        }
    }
}
