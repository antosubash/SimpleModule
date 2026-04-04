using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using SimpleModule.Tests.Shared.Fixtures;

namespace OpenIddict.Tests.Integration;

[Collection(TestCollections.Integration)]
public class ConnectEndpointTests
{
    private readonly SimpleModuleWebApplicationFactory _factory;

    public ConnectEndpointTests(SimpleModuleWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Authorize_Unauthenticated_ReturnsNonSuccess()
    {
        var client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }
        );

        var response = await client.GetAsync(
            "/connect/authorize?response_type=code&client_id=simplemodule-client&scope=openid&redirect_uri=https://localhost:5001/oauth-callback"
        );

        // OpenIddict validates the request via its own middleware pipeline;
        // without a registered client, this returns 400. With a valid client
        // but no authentication, it would redirect to login.
        response
            .StatusCode.Should()
            .BeOneOf(
                HttpStatusCode.BadRequest,
                HttpStatusCode.Redirect,
                HttpStatusCode.Unauthorized
            );
    }

    [Fact]
    public async Task Authorize_WithoutRequiredParams_ReturnsBadRequest()
    {
        var client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }
        );

        // Missing required OpenID Connect parameters
        var response = await client.GetAsync("/connect/authorize");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Userinfo_Unauthenticated_ReturnsBadRequestOrUnauthorized()
    {
        var client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }
        );

        var response = await client.GetAsync("/connect/userinfo");

        // OpenIddict rejects the request at the middleware level when no
        // valid access token is provided (returns 400 or 401)
        response
            .StatusCode.Should()
            .BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task EndSession_WithoutToken_ReturnsBadRequestOrSuccess()
    {
        var client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }
        );

        var response = await client.GetAsync("/connect/endsession");

        // OpenIddict validates the end-session request; without a valid
        // id_token_hint it may return 400, 200, or redirect
        response
            .StatusCode.Should()
            .BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.OK, HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task Token_WithoutCredentials_ReturnsBadRequest()
    {
        var client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }
        );

        // Token endpoint requires grant_type and credentials
        var response = await client.PostAsync("/connect/token", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ConnectEndpoints_AreRegistered()
    {
        var client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }
        );

        // Verify endpoints exist (they return 400 from OpenIddict validation,
        // not 404 which would indicate they aren't registered)
        var authorizeResponse = await client.GetAsync("/connect/authorize");
        var userinfoResponse = await client.GetAsync("/connect/userinfo");
        var endsessionResponse = await client.GetAsync("/connect/endsession");

        authorizeResponse.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
        userinfoResponse.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
        endsessionResponse.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
    }
}
