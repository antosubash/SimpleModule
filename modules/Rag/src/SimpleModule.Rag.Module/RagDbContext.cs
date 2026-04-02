using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SimpleModule.Database;
using SimpleModule.Rag.StructuredRag.Data;

namespace SimpleModule.Rag.Module;

public sealed class RagDbContext(
    DbContextOptions<RagDbContext> options,
    IOptions<DatabaseOptions> dbOptions
) : DbContext(options)
{
    public DbSet<CachedStructuredKnowledge> CachedStructuredKnowledge =>
        Set<CachedStructuredKnowledge>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(
            new EntityConfigurations.CachedStructuredKnowledgeConfiguration()
        );
        modelBuilder.ApplyModuleSchema(RagConstants.ModuleName, dbOptions.Value);
    }
}
