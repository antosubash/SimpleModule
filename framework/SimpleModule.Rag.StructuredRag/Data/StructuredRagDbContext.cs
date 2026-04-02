using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SimpleModule.Database;

namespace SimpleModule.Rag.StructuredRag.Data;

public sealed class StructuredRagDbContext(
    DbContextOptions<StructuredRagDbContext> options,
    IOptions<DatabaseOptions> dbOptions
) : DbContext(options)
{
    public DbSet<CachedStructuredKnowledge> CachedStructuredKnowledge =>
        Set<CachedStructuredKnowledge>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new CachedStructuredKnowledgeConfiguration());
        modelBuilder.ApplyModuleSchema("StructuredRag", dbOptions.Value);
    }
}
