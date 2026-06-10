using System.Security.Claims;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SimpleModule.Core.Authorization.Policies;
using SimpleModule.Core.Exceptions;

namespace SimpleModule.Core.Tests.Authorization;

public class AuthorizerTests
{
    private sealed record Widget(string OwnerId);

    private sealed record Gadget(string Name);

    private sealed class WidgetOwnerPolicy : IPolicy<Widget>
    {
        public Task<AuthorizationResult> AuthorizeAsync(
            ClaimsPrincipal user,
            string action,
            Widget resource,
            CancellationToken cancellationToken = default
        )
        {
            var result =
                resource.OwnerId == user.FindFirstValue("sub")
                    ? AuthorizationResult.Allow()
                    : AuthorizationResult.Deny("Not the owner");
            return Task.FromResult(result);
        }
    }

    private sealed class AlwaysAllowPolicy : IPolicy<Widget>
    {
        public Task<AuthorizationResult> AuthorizeAsync(
            ClaimsPrincipal user,
            string action,
            Widget resource,
            CancellationToken cancellationToken = default
        ) => Task.FromResult(AuthorizationResult.Allow());
    }

    private sealed class AlwaysDenyPolicy : IPolicy<Widget>
    {
        public Task<AuthorizationResult> AuthorizeAsync(
            ClaimsPrincipal user,
            string action,
            Widget resource,
            CancellationToken cancellationToken = default
        ) => Task.FromResult(AuthorizationResult.Deny("Denied by second policy"));
    }

    private sealed class DenyAsNotFoundPolicy : IPolicy<Widget>
    {
        public Task<AuthorizationResult> AuthorizeAsync(
            ClaimsPrincipal user,
            string action,
            Widget resource,
            CancellationToken cancellationToken = default
        ) => Task.FromResult(AuthorizationResult.DenyAsNotFound("Hidden"));
    }

    private static ClaimsPrincipal CreateUser(string userId) =>
        new(new ClaimsIdentity([new Claim("sub", userId)], "test"));

    private static Authorizer CreateAuthorizer(
        Action<IServiceCollection>? configureServices = null,
        Action<PolicyAuthorizationOptions>? configureOptions = null
    )
    {
        var services = new ServiceCollection();
        configureServices?.Invoke(services);
        var provider = services.BuildServiceProvider();

        var options = new PolicyAuthorizationOptions();
        configureOptions?.Invoke(options);

        return new Authorizer(provider, Options.Create(options));
    }

    [Fact]
    public async Task CheckAsync_OwnerMatches_Allows()
    {
        var authorizer = CreateAuthorizer(s => s.AddScoped<IPolicy<Widget>, WidgetOwnerPolicy>());

        var result = await authorizer.CheckAsync(
            CreateUser("user-1"),
            PolicyActions.Update,
            new Widget("user-1")
        );

        result.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task CheckAsync_OwnerDiffers_DeniesWithReason()
    {
        var authorizer = CreateAuthorizer(s => s.AddScoped<IPolicy<Widget>, WidgetOwnerPolicy>());

        var result = await authorizer.CheckAsync(
            CreateUser("user-2"),
            PolicyActions.Update,
            new Widget("user-1")
        );

        result.IsAllowed.Should().BeFalse();
        result.Reason.Should().Be("Not the owner");
    }

    [Fact]
    public async Task CheckAsync_NoPolicyRegistered_ThrowsMissingPolicyException()
    {
        var authorizer = CreateAuthorizer();

        var act = () =>
            authorizer.CheckAsync(CreateUser("user-1"), PolicyActions.View, new Gadget("g"));

        await act.Should()
            .ThrowAsync<MissingPolicyException>()
            .WithMessage("*IPolicy<Gadget>*");
    }

    [Fact]
    public async Task CheckAsync_MultiplePolicies_DenyWins()
    {
        var authorizer = CreateAuthorizer(s =>
        {
            s.AddScoped<IPolicy<Widget>, AlwaysAllowPolicy>();
            s.AddScoped<IPolicy<Widget>, AlwaysDenyPolicy>();
        });

        var result = await authorizer.CheckAsync(
            CreateUser("user-1"),
            PolicyActions.Update,
            new Widget("user-1")
        );

        result.IsAllowed.Should().BeFalse();
        result.Reason.Should().Be("Denied by second policy");
    }

    [Fact]
    public async Task CheckAsync_MultiplePolicies_AllAllow_Allows()
    {
        var authorizer = CreateAuthorizer(s =>
        {
            s.AddScoped<IPolicy<Widget>, AlwaysAllowPolicy>();
            s.AddScoped<IPolicy<Widget>, WidgetOwnerPolicy>();
        });

        var result = await authorizer.CheckAsync(
            CreateUser("user-1"),
            PolicyActions.Update,
            new Widget("user-1")
        );

        result.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task AuthorizeAsync_Allowed_DoesNotThrow()
    {
        var authorizer = CreateAuthorizer(s => s.AddScoped<IPolicy<Widget>, WidgetOwnerPolicy>());

        var act = () =>
            authorizer.AuthorizeAsync(
                CreateUser("user-1"),
                PolicyActions.Update,
                new Widget("user-1")
            );

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task AuthorizeAsync_DeniedNonViewAction_ThrowsForbiddenWithReason()
    {
        var authorizer = CreateAuthorizer(s => s.AddScoped<IPolicy<Widget>, WidgetOwnerPolicy>());

        var act = () =>
            authorizer.AuthorizeAsync(
                CreateUser("user-2"),
                PolicyActions.Update,
                new Widget("user-1")
            );

        await act.Should().ThrowAsync<ForbiddenException>().WithMessage("Not the owner");
    }

    [Fact]
    public async Task AuthorizeAsync_DeniedViewAction_ThrowsForbiddenByDefault()
    {
        // NotFoundActions is empty by default — DenyAsNotFound is the per-decision way
        // to surface 404; a plain Deny keeps its 403 + reason.
        var authorizer = CreateAuthorizer(s => s.AddScoped<IPolicy<Widget>, WidgetOwnerPolicy>());

        var act = () =>
            authorizer.AuthorizeAsync(
                CreateUser("user-2"),
                PolicyActions.View,
                new Widget("user-1")
            );

        await act.Should().ThrowAsync<ForbiddenException>().WithMessage("Not the owner");
    }

    [Fact]
    public async Task AuthorizeAsync_CustomNotFoundAction_ThrowsNotFound()
    {
        var authorizer = CreateAuthorizer(
            s => s.AddScoped<IPolicy<Widget>, WidgetOwnerPolicy>(),
            o => o.NotFoundActions.Add("archive")
        );

        var act = () =>
            authorizer.AuthorizeAsync(CreateUser("user-2"), "archive", new Widget("user-1"));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CheckAsync_DenyAsNotFound_CarriesFlagAndReason()
    {
        var authorizer = CreateAuthorizer(s => s.AddScoped<IPolicy<Widget>, DenyAsNotFoundPolicy>());

        var result = await authorizer.CheckAsync(
            CreateUser("user-1"),
            PolicyActions.Update,
            new Widget("user-1")
        );

        result.IsAllowed.Should().BeFalse();
        result.TreatAsNotFound.Should().BeTrue();
        result.Reason.Should().Be("Hidden");
    }

    [Fact]
    public async Task AuthorizeAsync_DenyAsNotFound_ThrowsNotFoundForAnyAction()
    {
        // The policy's per-decision flag wins even when the action is not in
        // NotFoundActions — no host/module options mutation required.
        var authorizer = CreateAuthorizer(s => s.AddScoped<IPolicy<Widget>, DenyAsNotFoundPolicy>());

        var act = () =>
            authorizer.AuthorizeAsync(
                CreateUser("user-1"),
                PolicyActions.Update,
                new Widget("user-1")
            );

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task AuthorizeAsync_ViewAddedToNotFoundActions_ThrowsNotFound()
    {
        // Host-level opt-in override: a listed action maps every denial to 404.
        var authorizer = CreateAuthorizer(
            s => s.AddScoped<IPolicy<Widget>, WidgetOwnerPolicy>(),
            o => o.NotFoundActions.Add(PolicyActions.View)
        );

        var act = () =>
            authorizer.AuthorizeAsync(
                CreateUser("user-2"),
                PolicyActions.View,
                new Widget("user-1")
            );

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
