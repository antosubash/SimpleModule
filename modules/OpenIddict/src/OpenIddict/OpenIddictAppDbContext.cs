using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SimpleModule.Database;

namespace SimpleModule.OpenIddict;

public class OpenIddictAppDbContext(
    DbContextOptions<OpenIddictAppDbContext> options,
    IOptions<DatabaseOptions> dbOptions
) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyModuleSchema("OpenIddict", dbOptions.Value);
    }
}
