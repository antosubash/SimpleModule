using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using SimpleModule.Tests.Shared.Fixtures;

namespace Users.Tests.Integration;

[Collection(TestCollections.Integration)]
public class ManagePasskeysEndpointTests
{
    private readonly SimpleModuleWebApplicationFactory _factory;

    public ManagePasskeysEndpointTests(SimpleModuleWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_WhenAuthenticated_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/Identity/Account/Manage/Passkeys");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_WhenUnauthenticated_RedirectsToLogin()
    {
        var client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }
        );

        var response = await client.GetAsync("/Identity/Account/Manage/Passkeys");

        response
            .StatusCode.Should()
            .BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.Found, HttpStatusCode.Unauthorized);
    }
}
