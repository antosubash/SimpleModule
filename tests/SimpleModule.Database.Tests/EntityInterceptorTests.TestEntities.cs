using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SimpleModule.Core.Entities;

namespace SimpleModule.Database.Tests;

public sealed partial class EntityInterceptorTests
{
    private sealed class TimestampedTestEntity : IHasCreationTime, IHasModificationTime
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }

    private sealed class AuditableTestEntity : IAuditable
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
    }

    private sealed class SoftDeleteTestEntity : ISoftDelete, IHasCreationTime
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }
    }

    private sealed class VersionedTestEntity : IVersioned
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Version { get; set; }
    }

    private sealed class ConcurrencyTestEntity : IHasConcurrencyStamp
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ConcurrencyStamp { get; set; } = string.Empty;
    }

    private sealed class MultiTenantTestEntity : IMultiTenant
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
    }

    private sealed class FullAuditableTestEntity : FullAuditableEntity<int>
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class EntityTestDbContext(
        DbContextOptions<EntityTestDbContext> options,
        IOptions<DatabaseOptions> dbOptions,
        ITenantContext? tenantContext = null
    ) : DbContext(options)
    {
        public DbSet<TimestampedTestEntity> TimestampedEntities => Set<TimestampedTestEntity>();
        public DbSet<AuditableTestEntity> AuditableEntities => Set<AuditableTestEntity>();
        public DbSet<SoftDeleteTestEntity> SoftDeleteEntities => Set<SoftDeleteTestEntity>();
        public DbSet<VersionedTestEntity> VersionedEntities => Set<VersionedTestEntity>();
        public DbSet<ConcurrencyTestEntity> ConcurrencyEntities => Set<ConcurrencyTestEntity>();
        public DbSet<MultiTenantTestEntity> MultiTenantEntities => Set<MultiTenantTestEntity>();
        public DbSet<FullAuditableTestEntity> FullAuditableEntities =>
            Set<FullAuditableTestEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TimestampedTestEntity>(e => e.HasKey(x => x.Id));
            modelBuilder.Entity<AuditableTestEntity>(e => e.HasKey(x => x.Id));
            modelBuilder.Entity<SoftDeleteTestEntity>(e => e.HasKey(x => x.Id));
            modelBuilder.Entity<VersionedTestEntity>(e => e.HasKey(x => x.Id));
            modelBuilder.Entity<ConcurrencyTestEntity>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.ConcurrencyStamp).HasMaxLength(50);
            });
            modelBuilder.Entity<MultiTenantTestEntity>(e => e.HasKey(x => x.Id));
            modelBuilder.Entity<FullAuditableTestEntity>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).ValueGeneratedOnAdd();
                e.Property(x => x.ConcurrencyStamp).HasMaxLength(50);
            });

            modelBuilder.ApplyModuleSchema("EntityTest", dbOptions.Value);

            if (tenantContext is not null)
            {
                modelBuilder.ApplyMultiTenantFilters(tenantContext);
            }
        }
    }

    private sealed class MultiTenantTestDbContext(
        DbContextOptions<MultiTenantTestDbContext> options,
        IOptions<DatabaseOptions> dbOptions,
        ITenantContext tenantContext
    ) : DbContext(options)
    {
        public DbSet<MultiTenantTestEntity> MultiTenantEntities => Set<MultiTenantTestEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MultiTenantTestEntity>(e => e.HasKey(x => x.Id));
            modelBuilder.ApplyModuleSchema("MultiTenantTest", dbOptions.Value);
            modelBuilder.ApplyMultiTenantFilters(tenantContext);
        }
    }
}
