using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace SimpleModule.Database.Tests;

public sealed partial class EntityInterceptorTests
{
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
        var tableName = fixture
            .Context.Model.FindEntityType(typeof(MultiTenantTestEntity))!
            .GetTableName();
        await fixture.Context.Database.ExecuteSqlRawAsync(
            "INSERT INTO \"" + tableName + "\" (\"Name\", \"TenantId\") VALUES ('B', 'tenant-b')"
        );
#pragma warning restore EF1003

        // Query should only return tenant-a's entities
        var results = await fixture.Context.MultiTenantEntities.ToListAsync();
        results.Should().HaveCount(1);
        results[0].Name.Should().Be("A");

        // IgnoreQueryFilters should return all
        var allResults = await fixture
            .Context.MultiTenantEntities.IgnoreQueryFilters()
            .ToListAsync();
        allResults.Should().HaveCount(2);
    }

    [Fact]
    public async Task MultiTenant_Filter_Reads_Executing_Context_Tenant_Across_Shared_Model()
    {
        // Regression for the cross-tenant leak: two contexts of the same type share one
        // cached EF model (same internal service provider) and one database (same
        // connection) but carry different tenants. The filter must read each executing
        // context's tenant — not the tenant frozen into the model when the first context
        // built it. Under the previous Expression.Constant(tenantContext) implementation,
        // contextB below would still filter by tenant-a and return the wrong row.
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        using var internalProvider = new ServiceCollection()
            .AddEntityFrameworkSqlite()
            .BuildServiceProvider();

        var dbOptions = Options.Create(
            new DatabaseOptions { DefaultConnection = "Data Source=:memory:" }
        );

        DbContextOptions<MultiTenantTestDbContext> BuildOptions() =>
            new DbContextOptionsBuilder<MultiTenantTestDbContext>()
                .UseSqlite(connection)
                .UseInternalServiceProvider(internalProvider)
                .Options;

        // Context for tenant-a builds the shared model first and seeds both tenants' rows.
        await using (
            var contextA = new MultiTenantTestDbContext(
                BuildOptions(),
                dbOptions,
                new TestTenantContext("tenant-a")
            )
        )
        {
            await contextA.Database.EnsureCreatedAsync();
            contextA.MultiTenantEntities.AddRange(
                new MultiTenantTestEntity { Name = "A", TenantId = "tenant-a" },
                new MultiTenantTestEntity { Name = "B", TenantId = "tenant-b" }
            );
            await contextA.SaveChangesAsync();

            var aResults = await contextA.MultiTenantEntities.ToListAsync();
            aResults.Should().ContainSingle().Which.Name.Should().Be("A");
        }

        // A second context reuses the cached model and shared data but with a different
        // tenant; it must see only tenant-b's row.
        await using (
            var contextB = new MultiTenantTestDbContext(
                BuildOptions(),
                dbOptions,
                new TestTenantContext("tenant-b")
            )
        )
        {
            var bResults = await contextB.MultiTenantEntities.ToListAsync();
            bResults.Should().ContainSingle().Which.Name.Should().Be("B");
        }
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
}
