using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimpleModule.Tenants.Contracts;

namespace SimpleModule.Tenants.EntityConfigurations;

public sealed class TenantHostConfiguration : IEntityTypeConfiguration<TenantHostEntity>
{
    public void Configure(EntityTypeBuilder<TenantHostEntity> builder)
    {
        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).ValueGeneratedOnAdd();
        builder.Property(h => h.HostName).IsRequired().HasMaxLength(512);
        builder.HasIndex(h => h.HostName).IsUnique();

        builder.HasData(GenerateSeedHosts());
    }

    private static TenantHostEntity[] GenerateSeedHosts()
    {
        var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        return
        [
            new TenantHostEntity
            {
                Id = TenantHostId.From(1),
                TenantId = TenantId.From(1),
                HostName = "acme.localhost",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now,
                ConcurrencyStamp = "seed-host-1",
            },
            new TenantHostEntity
            {
                Id = TenantHostId.From(2),
                TenantId = TenantId.From(1),
                HostName = "acme.local",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now,
                ConcurrencyStamp = "seed-host-2",
            },
            new TenantHostEntity
            {
                Id = TenantHostId.From(3),
                TenantId = TenantId.From(2),
                HostName = "contoso.localhost",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now,
                ConcurrencyStamp = "seed-host-3",
            },
        ];
    }
}
