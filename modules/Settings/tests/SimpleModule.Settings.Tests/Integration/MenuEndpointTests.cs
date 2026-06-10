using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SimpleModule.Settings;
using SimpleModule.Settings.Contracts;
using SimpleModule.Tests.Shared.Fixtures;

namespace Settings.Tests.Integration;

[Collection(TestCollections.Integration)]
public class MenuEndpointTests(SimpleModuleWebApplicationFactory factory)
{
    [Fact]
    public async Task GetMenus_Authenticated_Returns200()
    {
        // The menu tree is the public navigation — any authenticated user may read it.
        var client = factory.CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/settings/menus");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateMenuItem_WithManageMenus_Returns201()
    {
        var client = factory.CreateAuthenticatedClient([SettingsPermissions.ManageMenus]);
        var request = new CreateMenuItemRequest
        {
            Label = "Test Menu",
            Url = "/test",
            IsVisible = true,
        };
        var response = await client.PostAsJsonAsync("/api/settings/menus", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateMenuItem_WithoutManageMenus_ReturnsForbidden()
    {
        var client = factory.CreateAuthenticatedClient();
        var request = new CreateMenuItemRequest
        {
            Label = "Nope",
            Url = "/nope",
            IsVisible = true,
        };
        var response = await client.PostAsJsonAsync("/api/settings/menus", request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateMenuItem_NotFound_Returns404()
    {
        var client = factory.CreateAuthenticatedClient([SettingsPermissions.ManageMenus]);
        var request = new UpdateMenuItemRequest { Label = "Updated", IsVisible = true };
        var response = await client.PutAsJsonAsync("/api/settings/menus/99999", request);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteMenuItem_NotFound_Returns404()
    {
        var client = factory.CreateAuthenticatedClient([SettingsPermissions.ManageMenus]);
        var response = await client.DeleteAsync("/api/settings/menus/99999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteMenuItem_WithoutManageMenus_ReturnsForbidden()
    {
        var client = factory.CreateAuthenticatedClient();
        var response = await client.DeleteAsync("/api/settings/menus/99999");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetMenus_Unauthenticated_Returns401()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/settings/menus");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
