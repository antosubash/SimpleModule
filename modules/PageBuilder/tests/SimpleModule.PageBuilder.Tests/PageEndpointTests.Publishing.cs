using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SimpleModule.PageBuilder;
using SimpleModule.PageBuilder.Contracts;

namespace PageBuilder.Tests;

public partial class PageEndpointTests
{
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
}
