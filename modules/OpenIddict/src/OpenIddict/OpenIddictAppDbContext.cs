using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenIddict.EntityFrameworkCore.Models;
using SimpleModule.Database;

namespace SimpleModule.OpenIddict;

public class OpenIddictAppDbContext(
    DbContextOptions<OpenIddictAppDbContext> options,
    IOptions<DatabaseOptions> dbOptions
) : DbContext(options)
{
    public DbSet<OpenIddictEntityFrameworkCoreApplication> Applications => Set<OpenIddictEntityFrameworkCoreApplication>();
    public DbSet<OpenIddictEntityFrameworkCoreAuthorization> Authorizations => Set<OpenIddictEntityFrameworkCoreAuthorization>();
    public DbSet<OpenIddictEntityFrameworkCoreScope> Scopes => Set<OpenIddictEntityFrameworkCoreScope>();
    public DbSet<OpenIddictEntityFrameworkCoreToken> Tokens => Set<OpenIddictEntityFrameworkCoreToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyModuleSchema("OpenIddict", dbOptions.Value);
    }
}
