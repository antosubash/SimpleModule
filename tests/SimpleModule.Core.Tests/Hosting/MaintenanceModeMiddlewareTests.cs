using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SimpleModule.Hosting.Maintenance;

namespace SimpleModule.Core.Tests.Hosting;

public sealed class MaintenanceModeMiddlewareTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _sentinelPath;
    private readonly FileMaintenanceModeStore _store;
    private readonly StubHostEnvironment _environment;

    public MaintenanceModeMiddlewareTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "sm-maintenance-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(_tempDir);
        _sentinelPath = Path.Combine(_tempDir, "maintenance.json");

        _environment = new StubHostEnvironment { ContentRootPath = _tempDir };
        _store = new FileMaintenanceModeStore(
            Options.Create(new MaintenanceModeOptions { SentinelPath = _sentinelPath }),
            _environment,
            NullLogger<FileMaintenanceModeStore>.Instance
        );
    }

    [Fact]
    public async Task Passes_through_when_sentinel_absent()
    {
        var middleware = CreateMiddleware(out var nextCalled);
        var ctx = new DefaultHttpContext();

        await middleware.InvokeAsync(ctx);

        nextCalled().Should().BeTrue();
        ctx.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task Returns_503_with_retry_after_when_active()
    {
        await _store.EnableAsync(new MaintenanceModeState { RetryAfterSeconds = 120 });
        var middleware = CreateMiddleware(out var nextCalled);
        var ctx = new DefaultHttpContext { Response = { Body = new MemoryStream() } };

        await middleware.InvokeAsync(ctx);

        nextCalled().Should().BeFalse();
        ctx.Response.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
        ctx.Response.Headers["Retry-After"].ToString().Should().Be("120");
    }

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task Health_endpoints_are_exempted(string path)
    {
        await _store.EnableAsync(new MaintenanceModeState());
        var middleware = CreateMiddleware(out var nextCalled);
        var ctx = new DefaultHttpContext();
        ctx.Request.Path = path;

        await middleware.InvokeAsync(ctx);

        nextCalled().Should().BeTrue();
        ctx.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task Inertia_requests_get_json_503()
    {
        await _store.EnableAsync(
            new MaintenanceModeState { Message = "Migrating", RetryAfterSeconds = 30 }
        );
        var middleware = CreateMiddleware(out _);
        var responseBody = new MemoryStream();
        var ctx = new DefaultHttpContext { Response = { Body = responseBody } };
        ctx.Request.Headers["X-Inertia"] = "true";

        await middleware.InvokeAsync(ctx);

        ctx.Response.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
        ctx.Response.ContentType.Should().Be("application/json");
        ctx.Response.Headers["X-Inertia"].ToString().Should().Be("true");
        ctx.Response.Headers["Vary"].ToString().Should().Be("X-Inertia");

        responseBody.Position = 0;
        var doc = await JsonDocument.ParseAsync(responseBody);
        doc.RootElement.GetProperty("component").GetString().Should().Be("System/Maintenance");
        doc.RootElement.GetProperty("props")
            .GetProperty("message")
            .GetString()
            .Should()
            .Be("Migrating");
    }

    [Fact]
    public async Task Bypass_query_string_sets_cookie_and_redirects()
    {
        await _store.EnableAsync(
            new MaintenanceModeState
            {
                SecretHash = MaintenanceModeMiddleware.HashSecret("opensesame"),
            }
        );
        var middleware = CreateMiddleware(out var nextCalled);
        var ctx = new DefaultHttpContext { Response = { Body = new MemoryStream() } };
        ctx.Request.Path = "/admin";
        ctx.Request.QueryString = new QueryString("?sm_bypass=opensesame");

        await middleware.InvokeAsync(ctx);

        nextCalled().Should().BeFalse();
        ctx.Response.StatusCode.Should().Be(StatusCodes.Status302Found);
        ctx.Response.Headers["Location"].ToString().Should().Be("/admin");

        // The Set-Cookie header should carry HttpOnly and SameSite=Lax.
        var setCookie = ctx.Response.Headers["Set-Cookie"].ToString();
        setCookie.Should().Contain("sm_bypass=");
        setCookie.Should().Contain("httponly", because: "bypass cookie must not be JS-readable");
        setCookie.Should().Contain("samesite=lax");
    }

    [Fact]
    public async Task Bypass_query_string_with_wrong_secret_still_503s()
    {
        await _store.EnableAsync(
            new MaintenanceModeState { SecretHash = MaintenanceModeMiddleware.HashSecret("right") }
        );
        var middleware = CreateMiddleware(out var nextCalled);
        var ctx = new DefaultHttpContext { Response = { Body = new MemoryStream() } };
        ctx.Request.QueryString = new QueryString("?sm_bypass=wrong");

        await middleware.InvokeAsync(ctx);

        nextCalled().Should().BeFalse();
        ctx.Response.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
    }

    [Fact]
    public async Task Valid_bypass_cookie_lets_request_through()
    {
        var secretHash = MaintenanceModeMiddleware.HashSecret("opensesame");
        await _store.EnableAsync(new MaintenanceModeState { SecretHash = secretHash });
        var middleware = CreateMiddleware(out var nextCalled);
        var ctx = new DefaultHttpContext { Response = { Body = new MemoryStream() } };
        ctx.Request.Headers["Cookie"] = $"sm_bypass={secretHash}";

        await middleware.InvokeAsync(ctx);

        nextCalled().Should().BeTrue();
    }

    [Fact]
    public async Task Expired_until_lets_request_through()
    {
        await _store.EnableAsync(
            new MaintenanceModeState { Until = DateTimeOffset.UtcNow.AddSeconds(-1) }
        );
        var middleware = CreateMiddleware(out var nextCalled);
        var ctx = new DefaultHttpContext();

        await middleware.InvokeAsync(ctx);

        nextCalled().Should().BeTrue();
    }

    private MaintenanceModeMiddleware CreateMiddleware(out Func<bool> wasNextCalled)
    {
        var called = false;
        wasNextCalled = () => called;
        RequestDelegate next = _ =>
        {
            called = true;
            return Task.CompletedTask;
        };

        return new MaintenanceModeMiddleware(
            next,
            _store,
            Options.Create(new MaintenanceModeOptions { SentinelPath = _sentinelPath }),
            _environment
        );
    }

    public void Dispose()
    {
        _store.Dispose();
        try
        {
            Directory.Delete(_tempDir, recursive: true);
        }
        catch (IOException)
        {
            // Test teardown: leave the directory alone on Windows lock contention.
        }
    }

    private sealed class StubHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "tests";
        public string ContentRootPath { get; set; } = string.Empty;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
