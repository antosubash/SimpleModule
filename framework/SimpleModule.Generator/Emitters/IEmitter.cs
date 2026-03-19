using Microsoft.CodeAnalysis;

namespace SimpleModule.Generator;

internal interface IEmitter
{
    void Emit(SourceProductionContext context, DiscoveryData data);
}
