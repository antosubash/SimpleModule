using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SimpleModule.Core.Entities;
using SimpleModule.Database.Interceptors;
using SimpleModule.Database.SoftDelete;

namespace SimpleModule.Database.Tests;

public sealed class SoftDeleteTests
{
    [Fact]
    public async Task WithTrashed_Returns_Live_And_Soft_Deleted_Rows()
    {
        await using var fixture = CreateFixture();
        fixture.Context.Items.AddRange(
            new SoftDeleteItem { Name = "alive" },
            new SoftDeleteItem { Name = "trashed" }
        );
        await fixture.Context.SaveChangesAsync();

        var trashed = await fixture.Context.Items.FirstAsync(x => x.Name == "trashed");
        fixture.Context.Items.Remove(trashed);
        await fixture.Context.SaveChangesAsync();

        var visible = await fixture.Context.Items.ToListAsync();
        var withTrashed = await fixture.Context.Items.WithTrashed().ToListAsync();

        visible.Should().ContainSingle().Which.Name.Should().Be("alive");
        withTrashed.Should().HaveCount(2);
        withTrashed.Should().Contain(x => x.Name == "trashed" && x.IsDeleted);
    }

    [Fact]
    public async Task OnlyTrashed_Returns_Only_Soft_Deleted_Rows()
    {
        await using var fixture = CreateFixture();
        fixture.Context.Items.AddRange(
            new SoftDeleteItem { Name = "alive" },
            new SoftDeleteItem { Name = "trashed" }
        );
        await fixture.Context.SaveChangesAsync();

        var trashed = await fixture.Context.Items.FirstAsync(x => x.Name == "trashed");
        fixture.Context.Items.Remove(trashed);
        await fixture.Context.SaveChangesAsync();

        var only = await fixture.Context.Items.OnlyTrashed().ToListAsync();

        only.Should().ContainSingle().Which.Name.Should().Be("trashed");
        only[0].IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task RestoreAsync_Clears_Soft_Delete_Fields()
    {
        await using var fixture = CreateFixture("user-1");
        var item = new SoftDeleteItem { Name = "doomed" };
        fixture.Context.Items.Add(item);
        await fixture.Context.SaveChangesAsync();

        fixture.Context.Items.Remove(item);
        await fixture.Context.SaveChangesAsync();
        item.IsDeleted.Should().BeTrue();

        var service = fixture.GetService();
        var affected = await service.RestoreAsync(item.Id);

        affected.Should().Be(1);
        var restored = await fixture.Context.Items.FirstAsync(x => x.Id == item.Id);
        restored.IsDeleted.Should().BeFalse();
        restored.DeletedAt.Should().BeNull();
        restored.DeletedBy.Should().BeNull();
    }

    [Fact]
    public async Task RestoreAsync_Returns_Zero_When_Not_Trashed()
    {
        await using var fixture = CreateFixture();
        var item = new SoftDeleteItem { Name = "alive" };
        fixture.Context.Items.Add(item);
        await fixture.Context.SaveChangesAsync();

        var affected = await fixture.GetService().RestoreAsync(item.Id);

        affected.Should().Be(0);
    }

    [Fact]
    public async Task ForceDelete_Issues_Real_Delete_Bypassing_Soft_Delete()
    {
        await using var fixture = CreateFixture();
        var item = new SoftDeleteItem { Name = "purge-me" };
        fixture.Context.Items.Add(item);
        await fixture.Context.SaveChangesAsync();

        fixture.Context.ForceDelete(item);
        await fixture.Context.SaveChangesAsync();

        var any = await fixture.Context.Items.WithTrashed().AnyAsync(x => x.Id == item.Id);
        any.Should().BeFalse();
    }

    [Fact]
    public async Task ForceDeleteAsync_Removes_Trashed_Row()
    {
        await using var fixture = CreateFixture();
        var item = new SoftDeleteItem { Name = "two-step" };
        fixture.Context.Items.Add(item);
        await fixture.Context.SaveChangesAsync();

        fixture.Context.Items.Remove(item);
        await fixture.Context.SaveChangesAsync();

        var affected = await fixture.GetService().ForceDeleteAsync(item.Id);

        affected.Should().Be(1);
        var any = await fixture.Context.Items.WithTrashed().AnyAsync(x => x.Id == item.Id);
        any.Should().BeFalse();
    }

    [Fact]
    public async Task ForceDeleteRangeAsync_Removes_Multiple_Rows()
    {
        await using var fixture = CreateFixture();
        var a = new SoftDeleteItem { Name = "a" };
        var b = new SoftDeleteItem { Name = "b" };
        var c = new SoftDeleteItem { Name = "c" };
        fixture.Context.Items.AddRange(a, b, c);
        await fixture.Context.SaveChangesAsync();

        var affected = await fixture.GetService().ForceDeleteRangeAsync([a.Id, b.Id]);

        affected.Should().Be(2);
        var remaining = await fixture.Context.Items.WithTrashed().Select(x => x.Name).ToListAsync();
        remaining.Should().ContainSingle().Which.Should().Be("c");
    }

    [Fact]
    public async Task PurgeOlderThanAsync_Deletes_Only_Old_Trashed_Rows()
    {
        await using var fixture = CreateFixture();
        var fresh = new SoftDeleteItem { Name = "fresh" };
        var stale = new SoftDeleteItem { Name = "stale" };
        fixture.Context.Items.AddRange(fresh, stale);
        await fixture.Context.SaveChangesAsync();

        fixture.Context.Items.Remove(stale);
        await fixture.Context.SaveChangesAsync();
        stale.DeletedAt = DateTimeOffset.UtcNow - TimeSpan.FromDays(40);
        await fixture.Context.SaveChangesAsync();

        fixture.Context.Items.Remove(fresh);
        await fixture.Context.SaveChangesAsync();

        var purged = await fixture.GetService().PurgeOlderThanAsync(TimeSpan.FromDays(30));

        purged.Should().Be(1);
        var survivors = await fixture.Context.Items.WithTrashed().Select(x => x.Name).ToListAsync();
        survivors.Should().ContainSingle().Which.Should().Be("fresh");
    }

    [Fact]
    public async Task PurgeOlderThanAsync_Skips_Non_Trashed_Rows()
    {
        await using var fixture = CreateFixture();
        var alive = new SoftDeleteItem { Name = "alive" };
        fixture.Context.Items.Add(alive);
        await fixture.Context.SaveChangesAsync();

        var purged = await fixture.GetService().PurgeOlderThanAsync(TimeSpan.Zero);

        purged.Should().Be(0);
        (await fixture.Context.Items.AnyAsync(x => x.Id == alive.Id)).Should().BeTrue();
    }

    private static TestFixture CreateFixture(string? userId = null)
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
                new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId)], "TestAuth")
            );
        }

        services.AddSingleton<IHttpContextAccessor>(
            new HttpContextAccessor { HttpContext = httpContext }
        );
        services.AddScoped<ISaveChangesInterceptor, EntityInterceptor>();
        services.AddModuleDbContext<SoftDeleteTestDbContext>(config, "SoftDeleteTest");
        services.AddSoftDelete<SoftDeleteItem, SoftDeleteTestDbContext>();

        var provider = services.BuildServiceProvider();
        var context = provider.GetRequiredService<SoftDeleteTestDbContext>();
        context.Database.OpenConnection();
        context.Database.EnsureCreated();
        return new TestFixture(provider, context);
    }

    private sealed class TestFixture(ServiceProvider provider, SoftDeleteTestDbContext context)
        : IAsyncDisposable
    {
        public SoftDeleteTestDbContext Context => context;

        public ISoftDeleteService<SoftDeleteItem> GetService() =>
            provider.GetRequiredService<ISoftDeleteService<SoftDeleteItem>>();

        public async ValueTask DisposeAsync()
        {
            await context.DisposeAsync();
            await provider.DisposeAsync();
        }
    }

    public sealed class SoftDeleteItem : ISoftDelete
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsDeleted { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }
    }

    public sealed class SoftDeleteTestDbContext(
        DbContextOptions<SoftDeleteTestDbContext> options,
        IOptions<DatabaseOptions> dbOptions
    ) : DbContext(options)
    {
        public DbSet<SoftDeleteItem> Items => Set<SoftDeleteItem>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SoftDeleteItem>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).ValueGeneratedOnAdd();
            });
            modelBuilder.ApplyModuleSchema("SoftDeleteTest", dbOptions.Value);
        }
    }
}
