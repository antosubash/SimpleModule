using System.Net;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SimpleModule.Core.Authorization.Policies;
using SimpleModule.Core.Exceptions;

namespace SimpleModule.Core.Tests.Authorization;

/// <summary>
/// Shared TestServer host for the AuthorizeResource filter tests — booted once per
/// class. The environment is pinned so Development middleware (developer exception
/// page) can't swallow the exceptions the tests assert on.
/// </summary>
public sealed class AuthorizeResourceAppFixture : IAsyncLifetime
{
    public WebApplication App { get; private set; } = null!;
    public HttpClient Client { get; private set; } = null!;

    private sealed class DocPolicy : IPolicy<AuthorizeResourceFilterTests.Doc>
    {
        public Task<AuthorizationResult> AuthorizeAsync(
            ClaimsPrincipal user,
            string action,
            AuthorizeResourceFilterTests.Doc resource,
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

    private sealed class DocResolver : IResourceResolver<AuthorizeResourceFilterTests.Doc>
    {
        public ValueTask<AuthorizeResourceFilterTests.Doc?> ResolveAsync(
            string routeValue,
            CancellationToken cancellationToken = default
        ) =>
            ValueTask.FromResult(
                routeValue == "missing" ? null : new AuthorizeResourceFilterTests.Doc(routeValue)
            );
    }

    public async ValueTask InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder(
            new WebApplicationOptions { EnvironmentName = Environments.Production }
        );
        builder.WebHost.UseTestServer();
        builder.Services.AddOptions<PolicyAuthorizationOptions>();
        builder.Services.AddScoped<IAuthorizer, Authorizer>();
        builder.Services.AddScoped<IPolicy<AuthorizeResourceFilterTests.Doc>, DocPolicy>();
        builder.Services.AddScoped<
            IResourceResolver<AuthorizeResourceFilterTests.Doc>,
            DocResolver
        >();

        App = builder.Build();
        App.MapGet("/docs/{id}", (string id) => Results.Ok(id))
            .AuthorizeResource<AuthorizeResourceFilterTests.Doc>(PolicyActions.Update);
        App.MapGet("/misnamed/{docId}", (string docId) => Results.Ok(docId))
            .AuthorizeResource<AuthorizeResourceFilterTests.Doc>(PolicyActions.Update); // route param is NOT "id"
        App.MapGet("/optional/{id?}", (string? id) => Results.Ok(id))
            .AuthorizeResource<AuthorizeResourceFilterTests.Doc>(PolicyActions.Update);
        App.MapGet("/orphans/{id}", (string id) => Results.Ok(id))
            .AuthorizeResource<AuthorizeResourceFilterTests.Orphan>(PolicyActions.Update); // no resolver registered

        await App.StartAsync();
        Client = App.GetTestClient();
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await App.DisposeAsync();
    }
}

/// <summary>
/// Exercises the declarative AuthorizeResource endpoint filter against a real routing
/// pipeline: resolver dispatch, policy outcomes, and the failure paths — 404 for
/// runtime-absent values, loud InvalidOperationException for misconfiguration.
/// </summary>
public sealed class AuthorizeResourceFilterTests(AuthorizeResourceAppFixture fixture)
    : IClassFixture<AuthorizeResourceAppFixture>
{
    public sealed record Doc(string Kind);

    public sealed record Orphan(string Id);

    [Fact]
    public async Task AllowedResource_InvokesHandler()
    {
        var response = await fixture.Client.GetAsync("/docs/allow");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeniedResource_ThrowsForbidden()
    {
        var act = () => fixture.Client.GetAsync("/docs/deny");

        await act.Should().ThrowAsync<ForbiddenException>().WithMessage("Denied by DocPolicy");
    }

    [Fact]
    public async Task DenyAsNotFoundResource_ThrowsNotFound()
    {
        var act = () => fixture.Client.GetAsync("/docs/hide");

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task MissingResource_ThrowsNotFound()
    {
        var act = () => fixture.Client.GetAsync("/docs/missing");

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task OptionalParameterOmitted_ThrowsNotFound()
    {
        // The parameter exists in the template but wasn't supplied — runtime absence,
        // not misconfiguration.
        var act = () => fixture.Client.GetAsync("/optional");

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task WrongRouteParameterName_ThrowsInvalidOperation()
    {
        // Misconfiguration must fail loudly, not surface as 404.
        var act = () => fixture.Client.GetAsync("/misnamed/allow");

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*route parameter*");
    }

    [Fact]
    public async Task MissingResolver_ThrowsInvalidOperation()
    {
        var act = () => fixture.Client.GetAsync("/orphans/anything");

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*IResourceResolver*");
    }
}
