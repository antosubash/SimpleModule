using System.Net;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core.Authorization.Policies;
using SimpleModule.Core.Exceptions;

namespace SimpleModule.Core.Tests.Authorization;

/// <summary>
/// Exercises the declarative AuthorizeResource endpoint filter against a real routing
/// pipeline: resolver dispatch, policy outcomes, and the loud-failure paths for
/// misconfiguration (wrong route parameter name, missing resolver).
/// </summary>
public sealed class AuthorizeResourceFilterTests : IAsyncLifetime
{
    private WebApplication _app = null!;
    private HttpClient _client = null!;

    private sealed record Doc(string Kind);

    private sealed record Orphan(string Id);

    private sealed class DocPolicy : IPolicy<Doc>
    {
        public Task<AuthorizationResult> AuthorizeAsync(
            ClaimsPrincipal user,
            string action,
            Doc resource,
            CancellationToken cancellationToken = default
        ) =>
            Task.FromResult(
                resource.Kind switch
                {
                    "allow" => AuthorizationResult.Allow(),
                    "hide" => AuthorizationResult.DenyAsNotFound("Hidden"),
                    _ => AuthorizationResult.Deny("Denied by DocPolicy"),
                }
            );
    }

    private sealed class DocResolver : IResourceResolver<Doc>
    {
        public ValueTask<Doc?> ResolveAsync(
            string routeValue,
            CancellationToken cancellationToken = default
        ) => ValueTask.FromResult(routeValue == "missing" ? null : new Doc(routeValue));
    }

    public async ValueTask InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddOptions<PolicyAuthorizationOptions>();
        builder.Services.AddScoped<IAuthorizer, Authorizer>();
        builder.Services.AddScoped<IPolicy<Doc>, DocPolicy>();
        builder.Services.AddScoped<IResourceResolver<Doc>, DocResolver>();

        _app = builder.Build();
        _app.MapGet("/docs/{id}", (string id) => Results.Ok(id))
            .AuthorizeResource<Doc>(PolicyActions.Update);
        _app.MapGet("/misnamed/{docId}", (string docId) => Results.Ok(docId))
            .AuthorizeResource<Doc>(PolicyActions.Update); // route param is NOT "id"
        _app.MapGet("/orphans/{id}", (string id) => Results.Ok(id))
            .AuthorizeResource<Orphan>(PolicyActions.Update); // no resolver registered

        await _app.StartAsync();
        _client = _app.GetTestClient();
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _app.DisposeAsync();
    }

    [Fact]
    public async Task AllowedResource_InvokesHandler()
    {
        var response = await _client.GetAsync("/docs/allow");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeniedResource_ThrowsForbidden()
    {
        var act = () => _client.GetAsync("/docs/deny");

        await act.Should().ThrowAsync<ForbiddenException>().WithMessage("Denied by DocPolicy");
    }

    [Fact]
    public async Task DenyAsNotFoundResource_ThrowsNotFound()
    {
        var act = () => _client.GetAsync("/docs/hide");

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task MissingResource_ThrowsNotFound()
    {
        var act = () => _client.GetAsync("/docs/missing");

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task WrongRouteParameterName_ThrowsInvalidOperation()
    {
        // Misconfiguration must fail loudly, not surface as 404.
        var act = () => _client.GetAsync("/misnamed/allow");

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*route value*");
    }

    [Fact]
    public async Task MissingResolver_ThrowsInvalidOperation()
    {
        var act = () => _client.GetAsync("/orphans/anything");

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*IResourceResolver*");
    }
}
