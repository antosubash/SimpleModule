using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SimpleModule.PageBuilder.Contracts;

namespace PageBuilder.Tests;

public sealed partial class PageBuilderServiceTests
{
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
    public async Task DeletePage_SoftDeletes_SetsDeletedAt()
    {
        var page = await _sut.CreatePageAsync(new CreatePageRequest { Title = "To Soft Delete" });

        await _sut.DeletePageAsync(page.Id);

        // The soft-deleted row stays in storage with IsDeleted=true.
        var row = await _db.Pages.IgnoreQueryFilters().SingleAsync(p => p.Id == page.Id);
        row.IsDeleted.Should().BeTrue();
        row.DeletedAt.Should().NotBeNull();

        // And the trash list surfaces it.
        var trashed = await _sut.GetTrashedPagesAsync();
        trashed.Should().ContainSingle().Which.Title.Should().Be("To Soft Delete");
    }

    [Fact]
    public async Task RestorePage_ClearsDeletedAt()
    {
        var page = await _sut.CreatePageAsync(new CreatePageRequest { Title = "To Restore" });
        await _sut.DeletePageAsync(page.Id);

        var restored = await _sut.RestorePageAsync(page.Id);

        restored.DeletedAt.Should().BeNull();
        var found = await _sut.GetPageByIdAsync(restored.Id);
        found.Should().NotBeNull();
    }

    [Fact]
    public async Task PermanentDelete_RemovesCompletely()
    {
        var page = await _sut.CreatePageAsync(new CreatePageRequest { Title = "To Perm Delete" });
        await _sut.DeletePageAsync(page.Id);

        await _sut.PermanentDeletePageAsync(page.Id);

        var trashed = await _sut.GetTrashedPagesAsync();
        trashed.Should().BeEmpty();
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
        await _sut.UpdatePageContentAsync(
            page.Id,
            new UpdatePageContentRequest { Content = """{"content":[{"type":"Hero"}],"root":{}}""" }
        );
        var published = await _sut.PublishPageAsync(page.Id);
        published.Content.Should().Be("""{"content":[{"type":"Hero"}],"root":{}}""");
        published.DraftContent.Should().BeNull();
        published.IsPublished.Should().BeTrue();
    }

    [Fact]
    public async Task UpdatePage_DuplicateSlug_ThrowsArgumentException()
    {
        await _sut.CreatePageAsync(new CreatePageRequest { Title = "Page One", Slug = "page-one" });
        var page2 = await _sut.CreatePageAsync(
            new CreatePageRequest { Title = "Page Two", Slug = "page-two" }
        );

        var act = () =>
            _sut.UpdatePageAsync(
                page2.Id,
                new UpdatePageRequest
                {
                    Title = "Page Two",
                    Slug = "page-one",
                    Order = 0,
                    IsPublished = false,
                }
            );

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*already taken*");
    }
}
