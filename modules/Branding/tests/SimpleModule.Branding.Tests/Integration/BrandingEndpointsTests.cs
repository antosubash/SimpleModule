using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using SimpleModule.Branding.Contracts;
using SimpleModule.Tests.Shared.Fixtures;
using Xunit;

namespace Branding.Tests.Integration;

[Collection(TestCollections.Integration)]
public class BrandingEndpointsTests
{
    private readonly SimpleModuleWebApplicationFactory _factory;

    public BrandingEndpointsTests(SimpleModuleWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateAdminClient()
    {
        var client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }
        );
        var claims = $"{ClaimTypes.Role}=Admin;{ClaimTypes.NameIdentifier}=branding-admin-test";
        client.DefaultRequestHeaders.Add("X-Test-Claims", claims);
        client.DefaultRequestHeaders.Add("Authorization", "Bearer test-token");
        return client;
    }

    [Fact]
    public async Task Get_Requires_Permission()
    {
        var anon = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }
        );

        var res = await anon.GetAsync("/api/branding");

        res.StatusCode.Should()
            .BeOneOf(
                HttpStatusCode.Unauthorized,
                HttpStatusCode.Forbidden,
                HttpStatusCode.Redirect
            );
    }

    [Fact]
    public async Task Put_Then_Get_RoundTrips()
    {
        var client = CreateAdminClient();
        var model = new BrandingEditModel { AppName = "Acme Co", ColorPrimary = "#123456" };

        var put = await client.PutAsJsonAsync("/api/branding", model);
        put.StatusCode.Should().Be(HttpStatusCode.OK);

        var got = await client.GetFromJsonAsync<BrandingEditModel>("/api/branding");
        got.Should().NotBeNull();
        got!.AppName.Should().Be("Acme Co");
        got.ColorPrimary.Should().Be("#123456");
    }

    [Fact]
    public async Task Asset_Serve_Returns404_WhenUnset()
    {
        var anon = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }
        );

        var res = await anon.GetAsync("/api/branding/assets/favicon");

        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ManageView_RendersForAdmin()
    {
        var admin = CreateAdminClient();

        var res = await admin.GetAsync("/branding/manage");

        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
