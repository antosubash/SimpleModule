using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core;
using SimpleModule.Database;
using SimpleModule.Rag.Contracts;
using SimpleModule.Rag.StructuredRag.Data;

namespace SimpleModule.Rag.Module;

[Module(RagConstants.ModuleName)]
public class RagModule : IModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<RagDbContext>(configuration, RagConstants.ModuleName);
        services.AddScoped<IStructuredKnowledgeCache, EfStructuredKnowledgeCache>();
        services.AddScoped<IRagContracts, RagService>();
    }
}
