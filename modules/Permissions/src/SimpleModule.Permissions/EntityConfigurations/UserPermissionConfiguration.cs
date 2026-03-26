using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimpleModule.Permissions.Entities;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Permissions.EntityConfigurations;

public class UserPermissionConfiguration : IEntityTypeConfiguration<UserPermission>
{
    public void Configure(EntityTypeBuilder<UserPermission> builder)
    {
        builder.HasKey(e => new { e.UserId, e.Permission });
        builder.Property(e => e.UserId).HasConversion<UserId.EfCoreValueConverter>();
        builder.ToTable("UserPermissions");
    }
}
