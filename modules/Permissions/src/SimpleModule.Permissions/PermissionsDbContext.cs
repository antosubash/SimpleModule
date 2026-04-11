using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SimpleModule.Database;
using SimpleModule.Permissions.Contracts;
using SimpleModule.Permissions.EntityConfigurations;

namespace SimpleModule.Permissions;

public class PermissionsDbContext(
    DbContextOptions<PermissionsDbContext> options,
    IOptions<DatabaseOptions> dbOptions
) : DbContext(options)
{
    public DbSet<UserPermission> UserPermissions => Set<UserPermission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new UserPermissionConfiguration());
        modelBuilder.ApplyConfiguration(new RolePermissionConfiguration());

        modelBuilder.ApplyModuleSchema("Permissions", dbOptions.Value);
    }
}
