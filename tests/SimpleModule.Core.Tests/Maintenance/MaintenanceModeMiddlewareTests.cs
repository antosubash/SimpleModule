using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using SimpleModule.Core.Maintenance;
using SimpleModule.Hosting.Maintenance;
using SimpleModule.Hosting.Middleware;

namespace SimpleModule.Core.Tests.Maintenance;

public class MaintenanceModeMiddlewareTests
{
    private static readonly IOptions<MaintenanceModeOptions> Options =
        Microsoft.Extensions.Options.Options.Create(new MaintenanceModeOptions());

    [Fact]
    public async Task Passes_through_when_state_is_null()
    {
        var nextCalled = false;
        var middleware = new MaintenanceModeMiddleware(
            _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            new FakeStateProvider(state: null),
            Options
        );

        var context = NewContext(path: "/foo");
        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task Returns_503_when_active()
    {
        var middleware = new MaintenanceModeMiddleware(
            _ => throw new InvalidOperationException("next should not run"),
            new FakeStateProvider(new MaintenanceState { Active = true, RetryAfterSeconds = 42 }),
            Options
        );

        var context = NewContext(path: "/foo");
        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
        context.Response.Headers.RetryAfter.ToString().Should().Be("42");
    }

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task Exempts_health_check_paths(string path)
    {
        var nextCalled = false;
        var middleware = new MaintenanceModeMiddleware(
            _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            new FakeStateProvider(new MaintenanceState { Active = true }),
            Options
        );

        var context = NewContext(path);
        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task Bypass_query_with_valid_secret_sets_cookie_and_redirects()
    {
        const string secret = "let-me-in";
        var hash = MaintenanceModeMiddleware.HashSecret(secret);

        var middleware = new MaintenanceModeMiddleware(
            _ => Task.CompletedTask,
            new FakeStateProvider(new MaintenanceState { Active = true, SecretHash = hash }),
            Options
        );

        var context = NewContext(path: "/dashboard", query: $"?sm_bypass={secret}");
        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status302Found);
        context.Response.Headers.Location.ToString().Should().Be("/dashboard");
        var setCookie = context.Response.Headers.SetCookie.ToString();
        setCookie.Should().Contain("sm_bypass=");
        setCookie.Should().Contain("httponly", "cookie must be HttpOnly");
        setCookie.Should().ContainEquivalentOf("samesite=lax");
    }

    [Fact]
    public async Task Bypass_query_with_wrong_secret_still_returns_503()
    {
        var hash = MaintenanceModeMiddleware.HashSecret("the-correct-secret");

        var middleware = new MaintenanceModeMiddleware(
            _ => throw new InvalidOperationException("next should not run"),
            new FakeStateProvider(new MaintenanceState { Active = true, SecretHash = hash }),
            Options
        );

        var context = NewContext(path: "/dashboard", query: "?sm_bypass=wrong");
        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
    }

    [Fact]
    public async Task Bypass_cookie_with_matching_hash_passes_through()
    {
        const string secret = "let-me-in";
        var hash = MaintenanceModeMiddleware.HashSecret(secret);

        var nextCalled = false;
        var middleware = new MaintenanceModeMiddleware(
            _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            new FakeStateProvider(new MaintenanceState { Active = true, SecretHash = hash }),
            Options
        );

        var context = NewContext(path: "/dashboard");
        context.Request.Headers.Cookie = $"sm_bypass={hash}";
        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Inertia_request_gets_json_503()
    {
        var middleware = new MaintenanceModeMiddleware(
            _ => Task.CompletedTask,
            new FakeStateProvider(
                new MaintenanceState
                {
                    Active = true,
                    Message = "back soon",
                    RetryAfterSeconds = 30,
                }
            ),
            Options
        );

        var context = NewContext(path: "/inertia-page");
        context.Request.Headers["X-Inertia"] = "true";
        using var ms = new MemoryStream();
        context.Response.Body = ms;

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
        context.Response.ContentType.Should().StartWith("application/json");

        ms.Position = 0;
        var doc = await JsonDocument.ParseAsync(ms);
        doc.RootElement.GetProperty("message").GetString().Should().Be("back soon");
        doc.RootElement.GetProperty("retryAfterSeconds").GetInt32().Should().Be(30);
    }

    [Fact]
    public async Task Browser_request_gets_html_503()
    {
        var middleware = new MaintenanceModeMiddleware(
            _ => Task.CompletedTask,
            new FakeStateProvider(new MaintenanceState { Active = true, Message = "scheduled" }),
            Options
        );

        var context = NewContext(path: "/");
        context.Request.Headers.Accept = "text/html,application/xhtml+xml";
        using var ms = new MemoryStream();
        context.Response.Body = ms;

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
        context.Response.ContentType.Should().StartWith("text/html");
        Encoding.UTF8.GetString(ms.ToArray()).Should().Contain("503").And.Contain("scheduled");
    }

    private static DefaultHttpContext NewContext(string path, string? query = null)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Request.Path = path;
        if (query is not null)
        {
            context.Request.QueryString = new QueryString(query);
        }
        return context;
    }

    private sealed class FakeStateProvider(MaintenanceState? state) : IMaintenanceStateProvider
    {
        public ValueTask<MaintenanceState?> GetAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(state);
    }
}
