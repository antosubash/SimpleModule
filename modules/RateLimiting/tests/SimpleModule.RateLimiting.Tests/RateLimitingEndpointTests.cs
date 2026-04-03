using System.Net;
using System.Security.Claims;
using FluentAssertions;
using SimpleModule.Tests.Shared.Fixtures;

namespace SimpleModule.RateLimiting.Tests;

public class RateLimitingEndpointTests : IClassFixture<SimpleModuleWebApplicationFactory>
{
    private readonly SimpleModuleWebApplicationFactory _factory;

    public RateLimitingEndpointTests(SimpleModuleWebApplicationFactory factory) =>
        _factory = factory;

    [Fact]
    public async Task AdminPage_ReturnsOk_ForAuthenticatedAdmin()
    {
        using var client = _factory.CreateAuthenticatedClient(new Claim(ClaimTypes.Role, "Admin"));

        var response = await client.GetAsync("/rate-limiting");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("RateLimiting/Admin");
    }

    [Fact]
    public async Task ActivePoliciesApi_ReturnsOk_ForAuthenticatedAdmin()
    {
        using var client = _factory.CreateAuthenticatedClient(new Claim(ClaimTypes.Role, "Admin"));

        var response = await client.GetAsync("/api/rate-limiting/active");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("fixed-default");
    }

    [Fact]
    public async Task RulesApi_ReturnsOk_ForAuthenticatedAdmin()
    {
        using var client = _factory.CreateAuthenticatedClient(new Claim(ClaimTypes.Role, "Admin"));

        var response = await client.GetAsync("/api/rate-limiting");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
