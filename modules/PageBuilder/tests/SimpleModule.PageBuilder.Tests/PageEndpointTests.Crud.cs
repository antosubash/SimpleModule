using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SimpleModule.PageBuilder;
using SimpleModule.PageBuilder.Contracts;

namespace PageBuilder.Tests;

public partial class PageEndpointTests
{
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
}
