using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SimpleModule.PageBuilder;
using SimpleModule.PageBuilder.Contracts;
using SimpleModule.Tests.Shared.Fixtures;

namespace PageBuilder.Tests;

[Collection(TestCollections.Integration)]
public class PageEndpointTests
{
    private readonly SimpleModuleWebApplicationFactory _factory;

    public PageEndpointTests(SimpleModuleWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAllPages_WithViewPermission_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient([PageBuilderPermissions.View]);

        var response = await client.GetAsync("/api/pagebuilder");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAllPages_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/pagebuilder");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreatePage_WithPermission_Returns201()
    {
        var client = _factory.CreateAuthenticatedClient([PageBuilderPermissions.Create]);
        var request = new CreatePageRequest { Title = "Test Page" };

        var response = await client.PostAsJsonAsync("/api/pagebuilder", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var page = await response.Content.ReadFromJsonAsync<Page>();
        page.Should().NotBeNull();
        page!.Title.Should().Be("Test Page");
        page.Slug.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task UpdateContent_WithPermission_SavesPuckJson()
    {
        var client = _factory.CreateAuthenticatedClient([
            PageBuilderPermissions.Create,
            PageBuilderPermissions.Update,
        ]);

        var createResponse = await client.PostAsJsonAsync(
            "/api/pagebuilder",
            new CreatePageRequest { Title = "Content Page" }
        );
        var created = await createResponse.Content.ReadFromJsonAsync<Page>();

        var contentRequest = new UpdatePageContentRequest
        {
            Content = """{"content":[],"root":{}}""",
        };
        var response = await client.PutAsJsonAsync(
            $"/api/pagebuilder/{created!.Id}/content",
            contentRequest
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<Page>();
        updated!.DraftContent.Should().Be("""{"content":[],"root":{}}""");
    }

    [Fact]
    public async Task ViewerEndpoint_PublishedPage_ReturnsOk()
    {
        var client = _factory.CreateAuthenticatedClient([
            PageBuilderPermissions.Create,
            PageBuilderPermissions.Publish,
        ]);

        var createResponse = await client.PostAsJsonAsync(
            "/api/pagebuilder",
            new CreatePageRequest { Title = "Viewer Test", Slug = "viewer-test" }
        );
        var created = await createResponse.Content.ReadFromJsonAsync<Page>();

        await client.PostAsync($"/api/pagebuilder/{created!.Id}/publish", null);

        var anonClient = _factory.CreateClient();
        var response = await anonClient.GetAsync("/pages/view/viewer-test");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ViewerEndpoint_UnpublishedPage_Returns404()
    {
        var client = _factory.CreateAuthenticatedClient([PageBuilderPermissions.Create]);

        await client.PostAsJsonAsync(
            "/api/pagebuilder",
            new CreatePageRequest { Title = "Unpublished Page", Slug = "unpublished-page" }
        );

        var anonClient = _factory.CreateClient();
        var response = await anonClient.GetAsync("/pages/view/unpublished-page");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeletePage_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.DeleteAsync("/api/pagebuilder/1");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeletePage_WithoutPermission_Returns403()
    {
        var client = _factory.CreateAuthenticatedClient([PageBuilderPermissions.View]);

        var response = await client.DeleteAsync("/api/pagebuilder/1");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeletePage_WithPermission_Returns204()
    {
        var client = _factory.CreateAuthenticatedClient([
            PageBuilderPermissions.Create,
            PageBuilderPermissions.Delete,
        ]);

        var createResponse = await client.PostAsJsonAsync(
            "/api/pagebuilder",
            new CreatePageRequest { Title = "To Delete" }
        );
        var created = await createResponse.Content.ReadFromJsonAsync<Page>();

        var response = await client.DeleteAsync($"/api/pagebuilder/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // === Soft Delete Tests ===

    [Fact]
    public async Task DeletePage_SoftDeletes_StillInTrash()
    {
        var client = _factory.CreateAuthenticatedClient([
            PageBuilderPermissions.Create,
            PageBuilderPermissions.Delete,
        ]);

        var createRes = await client.PostAsJsonAsync(
            "/api/pagebuilder",
            new CreatePageRequest { Title = "Soft Delete Test" }
        );
        var created = await createRes.Content.ReadFromJsonAsync<Page>();

        // Soft delete
        var deleteRes = await client.DeleteAsync($"/api/pagebuilder/{created!.Id}");
        deleteRes.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Should be in trash
        var trashRes = await client.GetAsync("/api/pagebuilder/trash");
        trashRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var trashed = await trashRes.Content.ReadFromJsonAsync<PageSummary[]>();
        trashed.Should().Contain(p => p.Id == created.Id);
    }

    [Fact]
    public async Task RestorePage_ReturnsToNormalList()
    {
        var client = _factory.CreateAuthenticatedClient([
            PageBuilderPermissions.Create,
            PageBuilderPermissions.Delete,
            PageBuilderPermissions.View,
        ]);

        var createRes = await client.PostAsJsonAsync(
            "/api/pagebuilder",
            new CreatePageRequest { Title = "Restore Test" }
        );
        var created = await createRes.Content.ReadFromJsonAsync<Page>();

        await client.DeleteAsync($"/api/pagebuilder/{created!.Id}");

        var restoreRes = await client.PostAsync($"/api/pagebuilder/{created.Id}/restore", null);
        restoreRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // Should be back in normal list
        var listRes = await client.GetAsync("/api/pagebuilder");
        var pages = await listRes.Content.ReadFromJsonAsync<PageSummary[]>();
        pages.Should().Contain(p => p.Id == created.Id);
    }

    [Fact]
    public async Task PermanentDelete_RemovesFromTrash()
    {
        var client = _factory.CreateAuthenticatedClient([
            PageBuilderPermissions.Create,
            PageBuilderPermissions.Delete,
        ]);

        var createRes = await client.PostAsJsonAsync(
            "/api/pagebuilder",
            new CreatePageRequest { Title = "Perm Delete Test" }
        );
        var created = await createRes.Content.ReadFromJsonAsync<Page>();

        await client.DeleteAsync($"/api/pagebuilder/{created!.Id}");

        var permDeleteRes = await client.DeleteAsync($"/api/pagebuilder/{created.Id}/permanent");
        permDeleteRes.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var trashRes = await client.GetAsync("/api/pagebuilder/trash");
        var trashed = await trashRes.Content.ReadFromJsonAsync<PageSummary[]>();
        trashed.Should().NotContain(p => p.Id == created.Id);
    }

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

    // === Draft Tests ===

    [Fact]
    public async Task UpdateContent_SavesToDraft_PublishCopiesIt()
    {
        var client = _factory.CreateAuthenticatedClient([
            PageBuilderPermissions.Create,
            PageBuilderPermissions.Update,
            PageBuilderPermissions.Publish,
            PageBuilderPermissions.View,
        ]);

        var createRes = await client.PostAsJsonAsync(
            "/api/pagebuilder",
            new CreatePageRequest { Title = "Draft Flow Test" }
        );
        var created = await createRes.Content.ReadFromJsonAsync<Page>();

        // Save content to draft
        var draftContent = """{"content":[{"type":"Heading"}],"root":{}}""";
        await client.PutAsJsonAsync(
            $"/api/pagebuilder/{created!.Id}/content",
            new UpdatePageContentRequest { Content = draftContent }
        );

        // Verify draft is set but content unchanged
        var getRes = await client.GetAsync($"/api/pagebuilder/{created.Id}");
        var page = await getRes.Content.ReadFromJsonAsync<Page>();
        page!.DraftContent.Should().Be(draftContent);
        page.Content.Should().Be("{}");

        // Publish — should copy draft to content
        await client.PostAsync($"/api/pagebuilder/{created.Id}/publish", null);
        var published = await (
            await client.GetAsync($"/api/pagebuilder/{created.Id}")
        ).Content.ReadFromJsonAsync<Page>();
        published!.Content.Should().Be(draftContent);
        published.DraftContent.Should().BeNull();
    }

    // === Slug Validation Tests ===

    [Fact]
    public async Task CreatePage_InvalidSlug_Returns400()
    {
        var client = _factory.CreateAuthenticatedClient([PageBuilderPermissions.Create]);

        var res = await client.PostAsJsonAsync(
            "/api/pagebuilder",
            new CreatePageRequest { Title = "x", Slug = "ab" }
        );

        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
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
