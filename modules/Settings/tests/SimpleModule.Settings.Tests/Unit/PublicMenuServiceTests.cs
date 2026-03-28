using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SimpleModule.Database;
using SimpleModule.Settings;
using SimpleModule.Settings.Contracts;
using SimpleModule.Settings.Entities;
using SimpleModule.Settings.Services;

namespace Settings.Tests.Unit;

public sealed class PublicMenuServiceTests : IDisposable
{
    private readonly SettingsDbContext _db;
    private readonly PublicMenuService _service;
    private readonly MemoryCache _cache;

    public PublicMenuServiceTests()
    {
        var options = new DbContextOptionsBuilder<SettingsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var dbOptions = Options.Create(
            new DatabaseOptions { DefaultConnection = "Data Source=:memory:" }
        );
        _db = new SettingsDbContext(options, dbOptions);
        _db.Database.EnsureCreated();

        _cache = new MemoryCache(new MemoryCacheOptions());
        _service = new PublicMenuService(_db, _cache, Options.Create(new SettingsModuleOptions()));
    }

    [Fact]
    public async Task GetMenuTreeAsync_ReturnsEmptyList_WhenNoItems()
    {
        var result = await _service.GetMenuTreeAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMenuTreeAsync_BuildsNestedTree()
    {
        _db.PublicMenuItems.Add(
            new PublicMenuItemEntity
            {
                Id = 1,
                Label = "Parent",
                IsVisible = true,
                SortOrder = 0,
            }
        );
        _db.PublicMenuItems.Add(
            new PublicMenuItemEntity
            {
                Id = 2,
                ParentId = 1,
                Label = "Child",
                IsVisible = true,
                SortOrder = 0,
            }
        );
        await _db.SaveChangesAsync();

        var result = await _service.GetMenuTreeAsync();

        result.Should().HaveCount(1);
        result[0].Label.Should().Be("Parent");
        result[0].Children.Should().HaveCount(1);
        result[0].Children[0].Label.Should().Be("Child");
    }

    [Fact]
    public async Task GetMenuTreeAsync_ExcludesInvisibleItems()
    {
        _db.PublicMenuItems.Add(
            new PublicMenuItemEntity
            {
                Id = 1,
                Label = "Visible",
                IsVisible = true,
                SortOrder = 0,
            }
        );
        _db.PublicMenuItems.Add(
            new PublicMenuItemEntity
            {
                Id = 2,
                Label = "Hidden",
                IsVisible = false,
                SortOrder = 1,
            }
        );
        await _db.SaveChangesAsync();

        var result = await _service.GetMenuTreeAsync();

        result.Should().HaveCount(1);
        result[0].Label.Should().Be("Visible");
    }

    [Fact]
    public async Task GetHomePageUrlAsync_ReturnsNull_WhenNoHomePage()
    {
        _db.PublicMenuItems.Add(
            new PublicMenuItemEntity
            {
                Id = 1,
                Label = "Page",
                Url = "/some-page",
                IsVisible = true,
                IsHomePage = false,
                SortOrder = 0,
            }
        );
        await _db.SaveChangesAsync();

        var result = await _service.GetHomePageUrlAsync();

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetHomePageUrlAsync_ReturnsUrl_WhenHomePageSet()
    {
        _db.PublicMenuItems.Add(
            new PublicMenuItemEntity
            {
                Id = 1,
                Label = "Home",
                Url = "/home",
                IsVisible = true,
                IsHomePage = true,
                SortOrder = 0,
            }
        );
        await _db.SaveChangesAsync();

        var result = await _service.GetHomePageUrlAsync();

        result.Should().Be("/home");
    }

    [Fact]
    public async Task CreateAsync_RejectsDepthGreaterThan3()
    {
        _db.PublicMenuItems.Add(
            new PublicMenuItemEntity
            {
                Id = 1,
                Label = "Level 1",
                IsVisible = true,
                SortOrder = 0,
            }
        );
        _db.PublicMenuItems.Add(
            new PublicMenuItemEntity
            {
                Id = 2,
                ParentId = 1,
                Label = "Level 2",
                IsVisible = true,
                SortOrder = 0,
            }
        );
        _db.PublicMenuItems.Add(
            new PublicMenuItemEntity
            {
                Id = 3,
                ParentId = 2,
                Label = "Level 3",
                IsVisible = true,
                SortOrder = 0,
            }
        );
        await _db.SaveChangesAsync();

        var request = new CreateMenuItemRequest { ParentId = 3, Label = "Level 4" };

        var act = () => _service.CreateAsync(request);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*maximum*depth*");
    }

    [Fact]
    public async Task SetHomePageAsync_ClearsPreviousHomePage()
    {
        _db.PublicMenuItems.Add(
            new PublicMenuItemEntity
            {
                Id = 1,
                Label = "Old Home",
                IsVisible = true,
                IsHomePage = true,
                SortOrder = 0,
            }
        );
        _db.PublicMenuItems.Add(
            new PublicMenuItemEntity
            {
                Id = 2,
                Label = "New Home",
                IsVisible = true,
                IsHomePage = false,
                SortOrder = 1,
            }
        );
        await _db.SaveChangesAsync();

        await _service.SetHomePageAsync(2);

        var oldHome = await _db.PublicMenuItems.FindAsync(1);
        var newHome = await _db.PublicMenuItems.FindAsync(2);
        oldHome!.IsHomePage.Should().BeFalse();
        newHome!.IsHomePage.Should().BeTrue();
    }

    public void Dispose()
    {
        _cache.Dispose();
        _db.Dispose();
        GC.SuppressFinalize(this);
    }
}
