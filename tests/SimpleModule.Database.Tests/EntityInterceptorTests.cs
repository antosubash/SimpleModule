using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SimpleModule.Core.Entities;
using SimpleModule.Database.Interceptors;

namespace SimpleModule.Database.Tests;

public sealed class EntityInterceptorTests
{
    private const string TestUserId = "user-123";

    [Fact]
    public async Task Added_Entity_Sets_CreatedAt_And_UpdatedAt()
    {
        await using var fixture = CreateFixture();
        var entity = new TimestampedTestEntity { Name = "Test" };

        fixture.Context.TimestampedEntities.Add(entity);
        await fixture.Context.SaveChangesAsync();

        entity.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        entity.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Modified_Entity_Sets_UpdatedAt_But_Not_CreatedAt()
    {
        await using var fixture = CreateFixture();
        var entity = new TimestampedTestEntity { Name = "Test" };

        fixture.Context.TimestampedEntities.Add(entity);
        await fixture.Context.SaveChangesAsync();

        var originalCreatedAt = entity.CreatedAt;
        await Task.Delay(10);

        entity.Name = "Updated";
        await fixture.Context.SaveChangesAsync();

        entity.CreatedAt.Should().Be(originalCreatedAt);
        entity.UpdatedAt.Should().BeAfter(originalCreatedAt);
    }

    [Fact]
    public async Task Added_Auditable_Entity_Sets_CreatedBy_And_UpdatedBy()
    {
        await using var fixture = CreateFixture(TestUserId);
        var entity = new AuditableTestEntity { Name = "Test" };

        fixture.Context.AuditableEntities.Add(entity);
        await fixture.Context.SaveChangesAsync();

        entity.CreatedBy.Should().Be(TestUserId);
        entity.UpdatedBy.Should().Be(TestUserId);
    }

    [Fact]
    public async Task Modified_Auditable_Entity_Updates_UpdatedBy_Only()
    {
        await using var fixture = CreateFixture(TestUserId);
        var entity = new AuditableTestEntity { Name = "Test" };

        fixture.Context.AuditableEntities.Add(entity);
        await fixture.Context.SaveChangesAsync();

        entity.Name = "Updated";
        await fixture.Context.SaveChangesAsync();

        entity.CreatedBy.Should().Be(TestUserId);
        entity.UpdatedBy.Should().Be(TestUserId);
    }

    [Fact]
    public async Task Delete_SoftDelete_Entity_Converts_To_Modified()
    {
        await using var fixture = CreateFixture(TestUserId);
        var entity = new SoftDeleteTestEntity { Name = "Test" };

        fixture.Context.SoftDeleteEntities.Add(entity);
        await fixture.Context.SaveChangesAsync();

        fixture.Context.SoftDeleteEntities.Remove(entity);
        await fixture.Context.SaveChangesAsync();

        entity.IsDeleted.Should().BeTrue();
        entity.DeletedAt.Should().NotBeNull();
        entity.DeletedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        entity.DeletedBy.Should().Be(TestUserId);
    }

    [Fact]
    public async Task SoftDeleted_Entity_Excluded_By_Query_Filter()
    {
        await using var fixture = CreateFixture();
        var entity = new SoftDeleteTestEntity { Name = "Test" };

        fixture.Context.SoftDeleteEntities.Add(entity);
        await fixture.Context.SaveChangesAsync();

        fixture.Context.SoftDeleteEntities.Remove(entity);
        await fixture.Context.SaveChangesAsync();

        var results = await fixture.Context.SoftDeleteEntities.ToListAsync();
        results.Should().BeEmpty();

        // But it should still exist when ignoring query filters
        var allResults = await fixture.Context.SoftDeleteEntities.IgnoreQueryFilters().ToListAsync();
        allResults.Should().HaveCount(1);
    }

    [Fact]
    public async Task Added_Versioned_Entity_Gets_Version_1()
    {
        await using var fixture = CreateFixture();
        var entity = new VersionedTestEntity { Name = "Test" };

        fixture.Context.VersionedEntities.Add(entity);
        await fixture.Context.SaveChangesAsync();

        entity.Version.Should().Be(1);
    }

    [Fact]
    public async Task Modified_Versioned_Entity_Increments_Version()
    {
        await using var fixture = CreateFixture();
        var entity = new VersionedTestEntity { Name = "Test" };

        fixture.Context.VersionedEntities.Add(entity);
        await fixture.Context.SaveChangesAsync();
        entity.Version.Should().Be(1);

        entity.Name = "Updated";
        await fixture.Context.SaveChangesAsync();
        entity.Version.Should().Be(2);

        entity.Name = "Updated Again";
        await fixture.Context.SaveChangesAsync();
        entity.Version.Should().Be(3);
    }

    [Fact]
    public async Task Added_Entity_Gets_ConcurrencyStamp()
    {
        await using var fixture = CreateFixture();
        var entity = new ConcurrencyTestEntity { Name = "Test" };

        fixture.Context.ConcurrencyEntities.Add(entity);
        await fixture.Context.SaveChangesAsync();

        entity.ConcurrencyStamp.Should().NotBeNullOrEmpty();
        entity.ConcurrencyStamp.Should().HaveLength(32); // Guid without hyphens
    }

    [Fact]
    public async Task Modified_Entity_Gets_New_ConcurrencyStamp()
    {
        await using var fixture = CreateFixture();
        var entity = new ConcurrencyTestEntity { Name = "Test" };

        fixture.Context.ConcurrencyEntities.Add(entity);
        await fixture.Context.SaveChangesAsync();

        var originalStamp = entity.ConcurrencyStamp;

        entity.Name = "Updated";
        await fixture.Context.SaveChangesAsync();

        entity.ConcurrencyStamp.Should().NotBe(originalStamp);
    }

    [Fact]
    public async Task MultiTenant_Entity_Gets_TenantId_On_Add()
    {
        await using var fixture = CreateFixture(tenantId: "tenant-abc");
        var entity = new MultiTenantTestEntity { Name = "Test" };

        fixture.Context.MultiTenantEntities.Add(entity);
        await fixture.Context.SaveChangesAsync();

        entity.TenantId.Should().Be("tenant-abc");
    }

    [Fact]
    public async Task MultiTenant_Query_Filter_Restricts_To_Current_Tenant()
    {
        // Uses a dedicated DbContext type to avoid EF Core model caching issues
        // with the main EntityTestDbContext (which may be built without tenant context).
        var tenantContext = new TestTenantContext("tenant-a");
        await using var fixture = CreateMultiTenantFixture(tenantContext);

        // Insert tenant-a entity via the interceptor
        var entityA = new MultiTenantTestEntity { Name = "A" };
        fixture.Context.MultiTenantEntities.Add(entityA);
        await fixture.Context.SaveChangesAsync();

        // Insert tenant-b entity directly via SQL to bypass the interceptor.
        // The table name is from EF Core metadata, not user input.
#pragma warning disable EF1003 // Test-only raw SQL with no user input
        var tableName = fixture.Context.Model.FindEntityType(typeof(MultiTenantTestEntity))!.GetTableName();
        await fixture.Context.Database.ExecuteSqlRawAsync(
            "INSERT INTO \"" + tableName + "\" (\"Name\", \"TenantId\") VALUES ('B', 'tenant-b')");
#pragma warning restore EF1003

        // Query should only return tenant-a's entities
        var results = await fixture.Context.MultiTenantEntities.ToListAsync();
        results.Should().HaveCount(1);
        results[0].Name.Should().Be("A");

        // IgnoreQueryFilters should return all
        var allResults = await fixture.Context.MultiTenantEntities.IgnoreQueryFilters().ToListAsync();
        allResults.Should().HaveCount(2);
    }

    private static TestFixture<MultiTenantTestDbContext> CreateMultiTenantFixture(TestTenantContext tenantContext)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Database:DefaultConnection"] = "Data Source=:memory:",
                }
            )
            .Build();

        var services = new ServiceCollection();
        var httpContextAccessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        services.AddSingleton<IHttpContextAccessor>(httpContextAccessor);
        services.AddSingleton<ITenantContext>(tenantContext);
        services.AddScoped<ISaveChangesInterceptor, EntityInterceptor>();
        services.AddModuleDbContext<MultiTenantTestDbContext>(config, "MultiTenantTest");

        var provider = services.BuildServiceProvider();
        var context = provider.GetRequiredService<MultiTenantTestDbContext>();
        context.Database.OpenConnection();
        context.Database.EnsureCreated();

        return new TestFixture<MultiTenantTestDbContext>(provider, context);
    }

    [Fact]
    public async Task FullAuditableEntity_BaseClass_Works()
    {
        await using var fixture = CreateFixture(TestUserId);
        var entity = new FullAuditableTestEntity { Name = "Test" };

        fixture.Context.FullAuditableEntities.Add(entity);
        await fixture.Context.SaveChangesAsync();

        entity.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        entity.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        entity.CreatedBy.Should().Be(TestUserId);
        entity.UpdatedBy.Should().Be(TestUserId);
        entity.Version.Should().Be(1);
        entity.ConcurrencyStamp.Should().NotBeNullOrEmpty();
        entity.IsDeleted.Should().BeFalse();
    }

    private static TestFixture CreateFixture(string? userId = null, string? tenantId = null, TestTenantContext? tenantContext = null)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Database:DefaultConnection"] = "Data Source=:memory:",
                }
            )
            .Build();

        var services = new ServiceCollection();

        var httpContext = new DefaultHttpContext();
        if (userId is not null)
        {
            httpContext.User = new ClaimsPrincipal(
                new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, userId)],
                    "TestAuth"
                )
            );
        }

        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        services.AddSingleton<IHttpContextAccessor>(httpContextAccessor);

        var effectiveTenantContext = tenantContext ?? (tenantId is not null ? new TestTenantContext(tenantId) : null);
        if (effectiveTenantContext is not null)
        {
            services.AddSingleton<ITenantContext>(effectiveTenantContext);
        }

        services.AddScoped<ISaveChangesInterceptor, EntityInterceptor>();
        services.AddModuleDbContext<EntityTestDbContext>(config, "EntityTest");

        var provider = services.BuildServiceProvider();
        var context = provider.GetRequiredService<EntityTestDbContext>();
        context.Database.OpenConnection();
        context.Database.EnsureCreated();

        return new TestFixture(provider, context);
    }

    private sealed class TestFixture(ServiceProvider provider, EntityTestDbContext context)
        : IAsyncDisposable
    {
        public EntityTestDbContext Context => context;

        public async ValueTask DisposeAsync()
        {
            await context.DisposeAsync();
            await provider.DisposeAsync();
        }
    }

    private sealed class TestFixture<T>(ServiceProvider provider, T context)
        : IAsyncDisposable where T : DbContext
    {
        public T Context => context;

        public async ValueTask DisposeAsync()
        {
            await context.DisposeAsync();
            await provider.DisposeAsync();
        }
    }

    private sealed record TestTenantContext(string? TenantId) : ITenantContext;
}

// --- Test entities ---

public class TimestampedTestEntity : IHasCreationTime, IHasModificationTime
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class AuditableTestEntity : IAuditable
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}

public class SoftDeleteTestEntity : ISoftDelete, IHasCreationTime
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}

public class VersionedTestEntity : IVersioned
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Version { get; set; }
}

public class ConcurrencyTestEntity : IHasConcurrencyStamp
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ConcurrencyStamp { get; set; } = string.Empty;
}

public class MultiTenantTestEntity : IMultiTenant
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
}

public class FullAuditableTestEntity : FullAuditableEntity<int>
{
    public string Name { get; set; } = string.Empty;
}

public class EntityTestDbContext(
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
    public DbSet<FullAuditableTestEntity> FullAuditableEntities => Set<FullAuditableTestEntity>();

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

public class MultiTenantTestDbContext(
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
