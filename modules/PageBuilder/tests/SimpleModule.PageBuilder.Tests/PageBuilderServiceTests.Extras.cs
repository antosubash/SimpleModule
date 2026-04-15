using FluentAssertions;
using SimpleModule.PageBuilder;
using SimpleModule.PageBuilder.Contracts;

namespace PageBuilder.Tests;

public sealed partial class PageBuilderServiceTests
{
    // ---------- Slugify / Validation ----------

    [Fact]
    public void Slugify_HandlesSpecialCharacters()
    {
        PageBuilderService.Slugify("Hello World!").Should().Be("hello-world");
        PageBuilderService.Slugify("My  Page  Title").Should().Be("my-page-title");
        PageBuilderService.Slugify("Café & Résumé").Should().Be("caf-rsum");
    }

    [Fact]
    public void ValidateSlug_ValidSlug_ReturnsNull()
    {
        PageBuilderService.ValidateSlug("hello-world").Should().BeNull();
        PageBuilderService.ValidateSlug("abc").Should().BeNull();
        PageBuilderService.ValidateSlug("my-page-123").Should().BeNull();
    }

    [Fact]
    public void ValidateSlug_TooShort_ReturnsError()
    {
        PageBuilderService.ValidateSlug("ab").Should().NotBeNull();
    }

    [Fact]
    public void ValidateSlug_InvalidChars_ReturnsError()
    {
        PageBuilderService.ValidateSlug("Hello World").Should().NotBeNull();
        PageBuilderService.ValidateSlug("has_underscore").Should().NotBeNull();
    }

    // ---------- Templates ----------

    [Fact]
    public async Task CreateTemplate_SavesNameAndContent()
    {
        var template = await _sut.CreateTemplateAsync(
            new CreatePageTemplateRequest
            {
                Name = "Landing Page",
                Content = """{"content":[{"type":"Hero"}],"root":{}}""",
            }
        );

        template.Name.Should().Be("Landing Page");
        template.Content.Should().Contain("Hero");
    }

    [Fact]
    public async Task GetAllTemplates_ReturnsOrderedByName()
    {
        await _sut.CreateTemplateAsync(
            new CreatePageTemplateRequest { Name = "Zzz", Content = "{}" }
        );
        await _sut.CreateTemplateAsync(
            new CreatePageTemplateRequest { Name = "Aaa", Content = "{}" }
        );

        var templates = await _sut.GetAllTemplatesAsync();

        templates.First().Name.Should().Be("Aaa");
    }

    [Fact]
    public async Task DeleteTemplate_Removes()
    {
        var template = await _sut.CreateTemplateAsync(
            new CreatePageTemplateRequest { Name = "To Delete", Content = "{}" }
        );

        await _sut.DeleteTemplateAsync(template.Id);

        var all = await _sut.GetAllTemplatesAsync();
        all.Should().BeEmpty();
    }

    // ---------- Tags ----------

    [Fact]
    public async Task AddTagToPage_CreatesTagAndAssociates()
    {
        var page = await _sut.CreatePageAsync(new CreatePageRequest { Title = "Tagged Page" });

        await _sut.AddTagToPageAsync(page.Id, "blog");

        var tags = await _sut.GetAllTagsAsync();
        tags.Should().ContainSingle().Which.Name.Should().Be("blog");
    }

    [Fact]
    public async Task AddTagToPage_DuplicateTag_DoesNotDuplicate()
    {
        var page = await _sut.CreatePageAsync(new CreatePageRequest { Title = "Tagged Page" });

        await _sut.AddTagToPageAsync(page.Id, "blog");
        await _sut.AddTagToPageAsync(page.Id, "blog");

        var tags = await _sut.GetAllTagsAsync();
        tags.Should().ContainSingle();
    }

    [Fact]
    public async Task RemoveTagFromPage_RemovesAssociation()
    {
        var page = await _sut.CreatePageAsync(new CreatePageRequest { Title = "Tagged Page" });
        await _sut.AddTagToPageAsync(page.Id, "blog");

        var tags = await _sut.GetAllTagsAsync();
        await _sut.RemoveTagFromPageAsync(page.Id, tags.First().Id);

        var updatedPage = await _sut.GetPageByIdAsync(page.Id);
        updatedPage!.Tags.Should().BeEmpty();
    }
}
