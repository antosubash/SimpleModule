using Microsoft.CodeAnalysis;

namespace SimpleModule.Generator;

internal sealed class DiagnosticEmitter : IEmitter
{
    public void Emit(SourceProductionContext context, DiscoveryData data)
    {
        ModuleChecks.Run(context, data);
        DbContextChecks.Run(context, data);
        DependencyChecks.Run(context, data);
        ContractAndDtoChecks.Run(context, data);
        PermissionFeatureChecks.Run(context, data);
        EndpointChecks.Run(context, data);
    }
}
