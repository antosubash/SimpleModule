using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimpleModule.Tenants.Contracts;

namespace SimpleModule.Tenants.EntityConfigurations;

public sealed class TenantConfiguration : IEntityTypeConfiguration<TenantEntity>
{
    public void Configure(EntityTypeBuilder<TenantEntity> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedOnAdd();
        builder.Property(t => t.Name).IsRequired().HasMaxLength(256);
        builder.Property(t => t.Slug).IsRequired().HasMaxLength(128);
        builder.HasIndex(t => t.Slug).IsUnique();
        builder.Property(t => t.AdminEmail).HasMaxLength(256);
        builder.Property(t => t.EditionName).HasMaxLength(128);
        builder.Property(t => t.ConnectionString).HasMaxLength(1024);

        builder
            .HasMany(t => t.Hosts)
            .WithOne(h => h.Tenant)
            .HasForeignKey(h => h.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasData(GenerateSeedTenants());
    }

    private static TenantEntity[] GenerateSeedTenants()
    {
        var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        return
        [
            new TenantEntity
            {
                Id = TenantId.From(1),
                Name = "Acme Corporation",
                Slug = "acme",
                Status = TenantStatus.Active,
                AdminEmail = "admin@acme.com",
                EditionName = "Enterprise",
                CreatedAt = now,
                UpdatedAt = now,
                ConcurrencyStamp = "seed-acme",
            },
            new TenantEntity
            {
                Id = TenantId.From(2),
                Name = "Contoso Ltd",
                Slug = "contoso",
                Status = TenantStatus.Active,
                AdminEmail = "admin@contoso.com",
                EditionName = "Standard",
                CreatedAt = now,
                UpdatedAt = now,
                ConcurrencyStamp = "seed-contoso",
            },
            new TenantEntity
            {
                Id = TenantId.From(3),
                Name = "Suspended Corp",
                Slug = "suspended-corp",
                Status = TenantStatus.Suspended,
                AdminEmail = "admin@suspended.com",
                CreatedAt = now,
                UpdatedAt = now,
                ConcurrencyStamp = "seed-suspended",
            },
        ];
    }
}
