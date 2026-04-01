using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel.Connectors.InMemory;

namespace SimpleModule.Rag.VectorStore.InMemory;

public static class InMemoryVectorStoreExtensions
{
    public static IServiceCollection AddInMemoryVectorStore(this IServiceCollection services)
    {
        services.AddSingleton(
            typeof(Microsoft.Extensions.VectorData.VectorStore),
            _ => new InMemoryVectorStore()
        );
        return services;
    }
}
