using System.Net.Http.Json;
using System.Security.Claims;
using FluentAssertions;
using SimpleModule.Branding.Contracts;
using SimpleModule.Tests.Shared.Fixtures;
using Xunit;

namespace Branding.Tests.Integration;

[Collection(TestCollections.Integration)]
public class BrandingRenderingTests
{
    private readonly SimpleModuleWebApplicationFactory _factory;

    public BrandingRenderingTests(SimpleModuleWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateAdminClient()
    {
        var client = _factory.CreateClient();
        var claims = $"{ClaimTypes.Role}=Admin;{ClaimTypes.NameIdentifier}=branding-render-test";
        client.DefaultRequestHeaders.Add("X-Test-Claims", claims);
        client.DefaultRequestHeaders.Add("Authorization", "Bearer test-token");
        return client;
    }

    [Fact]
    public async Task FullPage_Includes_BrandingSharedProp()
    {
        var admin = CreateAdminClient();

        var html = await admin.GetStringAsync("/");

        html.Should().Contain("\"branding\"");
    }

    [Fact]
    public async Task FullPage_Injects_PrimaryColor_WhenChanged()
    {
        var admin = CreateAdminClient();
        await admin.PutAsJsonAsync(
            "/api/branding",
            new BrandingEditModel { ColorPrimary = "#abcdef" }
        );

        var html = await admin.GetStringAsync("/");

        html.Should().Contain("--color-primary:#abcdef");
    }
}
