using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SimpleModule.PageBuilder;
using SimpleModule.PageBuilder.Contracts;

namespace PageBuilder.Tests;

public partial class PageEndpointTests
{
    // === Templates Tests ===

    [Fact]
    public async Task CreateTemplate_Returns201()
    {
        var client = _factory.CreateAuthenticatedClient([PageBuilderPermissions.Create]);

        var res = await client.PostAsJsonAsync(
            "/api/pagebuilder/templates",
            new CreatePageTemplateRequest
            {
                Name = "Test Template",
                Content = """{"content":[{"type":"Hero"}],"root":{}}""",
            }
        );

        res.StatusCode.Should().Be(HttpStatusCode.Created);
        var template = await res.Content.ReadFromJsonAsync<PageTemplate>();
        template!.Name.Should().Be("Test Template");
    }

    [Fact]
    public async Task GetAllTemplates_ReturnsOk()
    {
        var client = _factory.CreateAuthenticatedClient([PageBuilderPermissions.View]);

        var res = await client.GetAsync("/api/pagebuilder/templates");

        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteTemplate_Returns204()
    {
        var client = _factory.CreateAuthenticatedClient([
            PageBuilderPermissions.Create,
            PageBuilderPermissions.Delete,
        ]);

        var createRes = await client.PostAsJsonAsync(
            "/api/pagebuilder/templates",
            new CreatePageTemplateRequest
            {
                Name = $"Del Template {DateTime.UtcNow.Ticks}",
                Content = "{}",
            }
        );
        var template = await createRes.Content.ReadFromJsonAsync<PageTemplate>();

        var deleteRes = await client.DeleteAsync($"/api/pagebuilder/templates/{template!.Id}");
        deleteRes.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // === Tags Tests ===

    [Fact]
    public async Task GetAllTags_ReturnsOk()
    {
        var client = _factory.CreateAuthenticatedClient([PageBuilderPermissions.View]);

        var res = await client.GetAsync("/api/pagebuilder/tags");

        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AddTagToPage_Returns204()
    {
        var client = _factory.CreateAuthenticatedClient([
            PageBuilderPermissions.Create,
            PageBuilderPermissions.Update,
        ]);

        var createRes = await client.PostAsJsonAsync(
            "/api/pagebuilder",
            new CreatePageRequest { Title = "Tag Test" }
        );
        var page = await createRes.Content.ReadFromJsonAsync<Page>();

        var tagRes = await client.PostAsJsonAsync(
            $"/api/pagebuilder/{page!.Id}/tags",
            new AddTagRequest { Name = "test-tag" }
        );
        tagRes.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // === Metadata Tests ===

    [Fact]
    public async Task UpdatePage_WithMetadata_SavesFields()
    {
        var client = _factory.CreateAuthenticatedClient([
            PageBuilderPermissions.Create,
            PageBuilderPermissions.Update,
        ]);

        var createRes = await client.PostAsJsonAsync(
            "/api/pagebuilder",
            new CreatePageRequest { Title = "Meta Test" }
        );
        var created = await createRes.Content.ReadFromJsonAsync<Page>();

        var updateRes = await client.PutAsJsonAsync(
            $"/api/pagebuilder/{created!.Id}",
            new UpdatePageRequest
            {
                Title = "Meta Test Updated",
                Slug = created.Slug,
                Order = 0,
                IsPublished = false,
                MetaDescription = "A test page description",
                MetaKeywords = "test, page",
                OgImage = "https://example.com/image.png",
            }
        );

        updateRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateRes.Content.ReadFromJsonAsync<Page>();
        updated!.MetaDescription.Should().Be("A test page description");
        updated.MetaKeywords.Should().Be("test, page");
        updated.OgImage.Should().Be("https://example.com/image.png");
    }
}
