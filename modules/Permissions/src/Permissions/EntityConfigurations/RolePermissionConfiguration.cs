using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimpleModule.Permissions.Contracts;
using SimpleModule.Permissions.Entities;

namespace SimpleModule.Permissions.EntityConfigurations;

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.HasKey(e => new { e.RoleId, e.Permission });
        builder.Property(e => e.RoleId).HasConversion<RoleId.EfCoreValueConverter>();
        builder.ToTable("RolePermissions");
    }
}
