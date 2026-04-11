using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using SimpleModule.Core.Exceptions;

namespace SimpleModule.Core.Tests;

public class GlobalExceptionHandlerTests
{
    private readonly GlobalExceptionHandler _handler = new(
        NullLogger<GlobalExceptionHandler>.Instance
    );

    private static DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<JsonDocument> ReadResponseBodyAsync(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        return await JsonDocument.ParseAsync(context.Response.Body);
    }

    [Fact]
    public async Task ValidationException_Returns400_WithErrors()
    {
        var context = CreateHttpContext();
        var errors = new Dictionary<string, string[]> { ["Name"] = ["Name is required"] };
        var exception = new ValidationException(errors);

        var handled = await _handler.TryHandleAsync(context, exception, CancellationToken.None);

        handled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(400);

        var doc = await ReadResponseBodyAsync(context);
        doc.RootElement.GetProperty("title").GetString().Should().Be("Validation Error");
        doc.RootElement.GetProperty("status").GetInt32().Should().Be(400);
        doc.RootElement.TryGetProperty("errors", out _).Should().BeTrue();
    }

    [Fact]
    public async Task NotFoundException_Returns404()
    {
        var context = CreateHttpContext();
        var exception = new NotFoundException("Product", 42);

        var handled = await _handler.TryHandleAsync(context, exception, CancellationToken.None);

        handled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(404);

        var doc = await ReadResponseBodyAsync(context);
        doc.RootElement.GetProperty("title").GetString().Should().Be("Not Found");
        doc.RootElement.GetProperty("detail").GetString().Should().Contain("42");
    }

    [Fact]
    public async Task ConflictException_Returns409()
    {
        var context = CreateHttpContext();
        var exception = new ConflictException("Duplicate entry");

        var handled = await _handler.TryHandleAsync(context, exception, CancellationToken.None);

        handled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(409);

        var doc = await ReadResponseBodyAsync(context);
        doc.RootElement.GetProperty("title").GetString().Should().Be("Conflict");
    }

    [Fact]
    public async Task UnhandledException_Returns500_WithGenericMessage()
    {
        var context = CreateHttpContext();
        var exception = new InvalidOperationException("Sensitive internal error details");

        var handled = await _handler.TryHandleAsync(context, exception, CancellationToken.None);

        handled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(500);

        var doc = await ReadResponseBodyAsync(context);
        doc.RootElement.GetProperty("title").GetString().Should().Be("Internal Server Error");
        // Should NOT leak the internal exception message
        doc.RootElement.GetProperty("detail")
            .GetString()
            .Should()
            .NotContain("Sensitive internal error details");
        doc.RootElement.GetProperty("detail")
            .GetString()
            .Should()
            .Be("An unexpected error occurred. Please try again later.");
    }

    [Fact]
    public async Task ValidationException_ReturnsDetailWithExceptionMessage()
    {
        var context = CreateHttpContext();
        var exception = new ValidationException();

        await _handler.TryHandleAsync(context, exception, CancellationToken.None);

        var doc = await ReadResponseBodyAsync(context);
        doc.RootElement.GetProperty("detail")
            .GetString()
            .Should()
            .Be("One or more validation errors occurred.");
    }

    [Fact]
    public async Task NotFoundException_IncludesEntityInfoInDetail()
    {
        var context = CreateHttpContext();
        var exception = new NotFoundException("User", "abc-123");

        await _handler.TryHandleAsync(context, exception, CancellationToken.None);

        var doc = await ReadResponseBodyAsync(context);
        doc.RootElement.GetProperty("detail")
            .GetString()
            .Should()
            .Be("User with ID abc-123 not found");
    }

    [Fact]
    public async Task ForbiddenException_Returns403()
    {
        var context = CreateHttpContext();
        var exception = new ForbiddenException("Access denied to admin panel");

        var handled = await _handler.TryHandleAsync(context, exception, CancellationToken.None);

        handled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(403);

        var doc = await ReadResponseBodyAsync(context);
        doc.RootElement.GetProperty("title").GetString().Should().Be("Forbidden");
        doc.RootElement.GetProperty("detail")
            .GetString()
            .Should()
            .Be("Access denied to admin panel");
    }

    [Fact]
    public async Task Handler_AlwaysReturnsTrue()
    {
        var context = CreateHttpContext();

        var result1 = await _handler.TryHandleAsync(
            context,
            new ValidationException(),
            CancellationToken.None
        );
        context = CreateHttpContext();
        var result2 = await _handler.TryHandleAsync(
            context,
            new InvalidOperationException("any"),
            CancellationToken.None
        );

        result1.Should().BeTrue();
        result2.Should().BeTrue();
    }

    [Fact]
    public async Task InertiaRequest_NotFoundException_ReturnsInertiaErrorPage()
    {
        var context = CreateHttpContext();
        context.Request.Headers["X-Inertia"] = "true";
        context.Request.Headers["X-Inertia-Version"] = "1";
        var exception = new NotFoundException("Product", 42);

        var handled = await _handler.TryHandleAsync(context, exception, CancellationToken.None);

        handled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(404);
        context.Response.Headers["X-Inertia"].ToString().Should().Be("true");
        context.Response.ContentType.Should().Contain("application/json");

        var doc = await ReadResponseBodyAsync(context);
        doc.RootElement.GetProperty("component").GetString().Should().Be("Error/404");
        doc.RootElement.GetProperty("props").GetProperty("status").GetInt32().Should().Be(404);
        doc.RootElement.GetProperty("props")
            .GetProperty("title")
            .GetString()
            .Should()
            .Be("Not Found");
    }

    [Fact]
    public async Task NonInertiaRequest_NotFoundException_ReturnsJsonProblemDetails()
    {
        var context = CreateHttpContext();
        var exception = new NotFoundException("Product", 42);

        var handled = await _handler.TryHandleAsync(context, exception, CancellationToken.None);

        handled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(404);

        var doc = await ReadResponseBodyAsync(context);
        doc.RootElement.GetProperty("title").GetString().Should().Be("Not Found");
        doc.RootElement.TryGetProperty("component", out _).Should().BeFalse();
    }

    [Fact]
    public async Task InertiaRequest_UnhandledException_ReturnsInertia500_WithoutSensitiveDetails()
    {
        var context = CreateHttpContext();
        context.Request.Headers["X-Inertia"] = "true";
        context.Request.Headers["X-Inertia-Version"] = "1";
        var exception = new InvalidOperationException("Sensitive DB error");

        var handled = await _handler.TryHandleAsync(context, exception, CancellationToken.None);

        handled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(500);

        var doc = await ReadResponseBodyAsync(context);
        doc.RootElement.GetProperty("component").GetString().Should().Be("Error/500");
        var props = doc.RootElement.GetProperty("props");
        props.GetProperty("message").GetString().Should().NotContain("Sensitive DB error");
        props
            .GetProperty("message")
            .GetString()
            .Should()
            .Be("An unexpected error occurred. Please try again later.");
    }
}
