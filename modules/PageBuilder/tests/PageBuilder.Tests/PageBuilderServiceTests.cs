using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SimpleModule.Core.Exceptions;
using SimpleModule.Database;
using SimpleModule.PageBuilder;
using SimpleModule.PageBuilder.Contracts;

namespace PageBuilder.Tests;

public sealed class PageBuilderServiceTests : IDisposable
{
    private readonly PageBuilderDbContext _db;
    private readonly PageBuilderService _sut;

    public PageBuilderServiceTests()
    {
        var options = new DbContextOptionsBuilder<PageBuilderDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        var dbOptions = Options.Create(
            new DatabaseOptions
            {
                ModuleConnections = new Dictionary<string, string>
                {
                    ["PageBuilder"] = "Data Source=:memory:",
                },
            }
        );
        _db = new PageBuilderDbContext(options, dbOptions);
        _db.Database.OpenConnection();
        _db.Database.EnsureCreated();
        _sut = new PageBuilderService(_db, NullLogger<PageBuilderService>.Instance);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task CreatePage_GeneratesSlugFromTitle()
    {
        var request = new CreatePageRequest { Title = "Hello World" };

        var page = await _sut.CreatePageAsync(request);

        page.Title.Should().Be("Hello World");
        page.Slug.Should().Be("hello-world");
        page.Content.Should().Be("{}");
        page.IsPublished.Should().BeFalse();
    }

    [Fact]
    public async Task CreatePage_DuplicateSlug_AppendsNumber()
    {
        await _sut.CreatePageAsync(new CreatePageRequest { Title = "Test Page" });

        var second = await _sut.CreatePageAsync(new CreatePageRequest { Title = "Test Page" });

        second.Slug.Should().Be("test-page-1");
    }

    [Fact]
    public async Task UpdateContent_SavesJsonAndUpdatesTimestamp()
    {
        var page = await _sut.CreatePageAsync(new CreatePageRequest { Title = "Content Test" });
        var before = page.UpdatedAt;

        var updated = await _sut.UpdatePageContentAsync(
            page.Id,
            new UpdatePageContentRequest { Content = """{"content":[],"root":{}}""" }
        );

        updated.DraftContent.Should().Be("""{"content":[],"root":{}}""");
        updated.UpdatedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public async Task PublishPage_SetsFlag()
    {
        var page = await _sut.CreatePageAsync(new CreatePageRequest { Title = "Publish Test" });
        page.IsPublished.Should().BeFalse();

        var published = await _sut.PublishPageAsync(page.Id);

        published.IsPublished.Should().BeTrue();
    }

    [Fact]
    public async Task GetPublishedPages_FiltersCorrectly()
    {
        await _sut.CreatePageAsync(new CreatePageRequest { Title = "Draft" });
        var pub = await _sut.CreatePageAsync(new CreatePageRequest { Title = "Published" });
        await _sut.PublishPageAsync(pub.Id);

        var published = await _sut.GetPublishedPagesAsync();

        published.Should().ContainSingle().Which.Title.Should().Be("Published");
    }

    [Fact]
    public async Task DeletePage_Removes()
    {
        var page = await _sut.CreatePageAsync(new CreatePageRequest { Title = "To Delete" });

        await _sut.DeletePageAsync(page.Id);

        var found = await _sut.GetPageByIdAsync(page.Id);
        found.Should().BeNull();
    }

    [Fact]
    public async Task DeletePage_NonExistent_ThrowsNotFoundException()
    {
        var act = () => _sut.DeletePageAsync(PageId.From(99999));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UnpublishPage_ClearsFlag()
    {
        var page = await _sut.CreatePageAsync(new CreatePageRequest { Title = "Unpublish Test" });
        await _sut.PublishPageAsync(page.Id);

        var unpublished = await _sut.UnpublishPageAsync(page.Id);

        unpublished.IsPublished.Should().BeFalse();
    }

    [Fact]
    public async Task CreatePage_DraftContentIsNull()
    {
        var page = await _sut.CreatePageAsync(new CreatePageRequest { Title = "Draft Test" });
        page.DraftContent.Should().BeNull();
    }

    [Fact]
    public async Task UpdateContent_SavesToDraftContent_NotContent()
    {
        var page = await _sut.CreatePageAsync(new CreatePageRequest { Title = "Draft Save" });
        var updated = await _sut.UpdatePageContentAsync(
            page.Id,
            new UpdatePageContentRequest { Content = """{"content":[{"type":"Text"}],"root":{}}""" }
        );
        updated.DraftContent.Should().Be("""{"content":[{"type":"Text"}],"root":{}}""");
        updated.Content.Should().Be("{}");
    }

    [Fact]
    public async Task PublishPage_CopiesDraftToContent_ClearsDraft()
    {
        var page = await _sut.CreatePageAsync(new CreatePageRequest { Title = "Publish Draft" });
        await _sut.UpdatePageContentAsync(page.Id, new UpdatePageContentRequest { Content = """{"content":[{"type":"Hero"}],"root":{}}""" });
        var published = await _sut.PublishPageAsync(page.Id);
        published.Content.Should().Be("""{"content":[{"type":"Hero"}],"root":{}}""");
        published.DraftContent.Should().BeNull();
        published.IsPublished.Should().BeTrue();
    }

    [Fact]
    public void Slugify_HandlesSpecialCharacters()
    {
        PageBuilderService.Slugify("Hello World!").Should().Be("hello-world");
        PageBuilderService.Slugify("My  Page  Title").Should().Be("my-page-title");
        PageBuilderService.Slugify("Café & Résumé").Should().Be("caf-rsum");
    }
}
