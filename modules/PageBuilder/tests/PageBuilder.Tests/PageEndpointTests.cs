using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SimpleModule.PageBuilder;
using SimpleModule.PageBuilder.Contracts;
using SimpleModule.Tests.Shared.Fixtures;

namespace PageBuilder.Tests;

public class PageEndpointTests : IClassFixture<SimpleModuleWebApplicationFactory>
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
        var client = _factory.CreateAuthenticatedClient(
            [PageBuilderPermissions.Create, PageBuilderPermissions.Update]
        );

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
        var client = _factory.CreateAuthenticatedClient(
            [PageBuilderPermissions.Create, PageBuilderPermissions.Publish]
        );

        var createResponse = await client.PostAsJsonAsync(
            "/api/pagebuilder",
            new CreatePageRequest { Title = "Viewer Test", Slug = "viewer-test" }
        );
        var created = await createResponse.Content.ReadFromJsonAsync<Page>();

        await client.PostAsync($"/api/pagebuilder/{created!.Id}/publish", null);

        var anonClient = _factory.CreateClient();
        var response = await anonClient.GetAsync("/p/viewer-test");
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
        var response = await anonClient.GetAsync("/p/unpublished-page");
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
        var client = _factory.CreateAuthenticatedClient(
            [PageBuilderPermissions.Create, PageBuilderPermissions.Delete]
        );

        var createResponse = await client.PostAsJsonAsync(
            "/api/pagebuilder",
            new CreatePageRequest { Title = "To Delete" }
        );
        var created = await createResponse.Content.ReadFromJsonAsync<Page>();

        var response = await client.DeleteAsync($"/api/pagebuilder/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
