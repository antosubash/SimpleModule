using FluentAssertions;
using Microsoft.EntityFrameworkCore;

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
