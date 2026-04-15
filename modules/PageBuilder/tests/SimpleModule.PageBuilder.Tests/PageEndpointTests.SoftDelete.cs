using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SimpleModule.PageBuilder;
using SimpleModule.PageBuilder.Contracts;

namespace PageBuilder.Tests;

public partial class PageEndpointTests
{
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
}
