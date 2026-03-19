using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SimpleModule.Database;
using SimpleModule.Permissions.Contracts;
using SimpleModule.Permissions.Entities;
using SimpleModule.Users.Contracts;

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

        modelBuilder.Entity<UserPermission>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.Permission });
            entity.Property(e => e.UserId).HasConversion<UserId.EfCoreValueConverter>();
            entity.ToTable("UserPermissions");
        });

        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(e => new { e.RoleId, e.Permission });
            entity.Property(e => e.RoleId).HasConversion<RoleId.EfCoreValueConverter>();
            entity.ToTable("RolePermissions");
        });

        modelBuilder.ApplyModuleSchema("Permissions", dbOptions.Value);
    }
}
