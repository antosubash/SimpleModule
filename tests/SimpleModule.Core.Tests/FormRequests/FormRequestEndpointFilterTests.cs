using System.Security.Claims;
using System.Text.Json;
using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using SimpleModule.Core.FormRequests;

namespace SimpleModule.Core.Tests.FormRequests;

public class FormRequestEndpointFilterTests
{
    private readonly FormRequestEndpointFilter _filter = new();

    [Fact]
    public async Task InvokeAsync_NoFormRequestArguments_CallsNext()
    {
        var nextCalled = false;
        var context = CreateFilterContext("plain string argument", 42);

        await _filter.InvokeAsync(
            context,
            _ =>
            {
                nextCalled = true;
                return ValueTask.FromResult<object?>(Results.Ok());
            }
        );

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_ValidRequest_CallsNext()
    {
        var request = new ValidTestRequest { Name = "Test", Value = 10 };
        var nextCalled = false;
        var context = CreateFilterContext(request);

        await _filter.InvokeAsync(
            context,
            _ =>
            {
                nextCalled = true;
                return ValueTask.FromResult<object?>(Results.Ok());
            }
        );

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_InvalidRequest_Returns422()
    {
        var request = new ValidTestRequest { Name = "", Value = -1 };
        var context = CreateFilterContext(request);

        var result = await _filter.InvokeAsync(
            context,
            _ => ValueTask.FromResult<object?>(Results.Ok())
        );

        result.Should().BeAssignableTo<ProblemHttpResult>();
        var problem = (ProblemHttpResult)result!;
        problem.StatusCode.Should().Be(422);
        problem.ProblemDetails.Title.Should().Be("Validation Error");
        problem.ProblemDetails.Extensions.Should().ContainKey("errors");
    }

    [Fact]
    public async Task InvokeAsync_InvalidRequest_ReturnsFieldErrors()
    {
        var request = new ValidTestRequest { Name = "", Value = -1 };
        var context = CreateFilterContext(request);

        var result = await _filter.InvokeAsync(
            context,
            _ => ValueTask.FromResult<object?>(Results.Ok())
        );

        var problem = (ProblemHttpResult)result!;
        var errorsObj = problem.ProblemDetails.Extensions["errors"];
        var errorsJson = JsonSerializer.Serialize(errorsObj);
        var errors = JsonSerializer.Deserialize<Dictionary<string, string[]>>(errorsJson)!;
        errors.Should().ContainKey("Name");
        errors.Should().ContainKey("Value");
    }

    [Fact]
    public async Task InvokeAsync_UnauthorizedRequest_Returns403()
    {
        var request = new UnauthorizedTestRequest { Name = "Test" };
        var context = CreateFilterContext(request);

        var result = await _filter.InvokeAsync(
            context,
            _ => ValueTask.FromResult<object?>(Results.Ok())
        );

        result.Should().BeAssignableTo<ProblemHttpResult>();
        var problem = (ProblemHttpResult)result!;
        problem.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task InvokeAsync_Prepare_NormalizesDataBeforeValidation()
    {
        var request = new PrepareTestRequest { Sku = "  abc-123  " };
        var context = CreateFilterContext(request);

        await _filter.InvokeAsync(context, _ => ValueTask.FromResult<object?>(Results.Ok()));

        request.Sku.Should().Be("ABC-123");
    }

    [Fact]
    public async Task InvokeAsync_Prepare_RunsBeforeValidation()
    {
        var request = new PrepareTestRequest { Sku = "  valid-sku  " };
        var context = CreateFilterContext(request);
        var nextCalled = false;

        await _filter.InvokeAsync(
            context,
            _ =>
            {
                nextCalled = true;
                return ValueTask.FromResult<object?>(Results.Ok());
            }
        );

        nextCalled.Should().BeTrue();
        request.Sku.Should().Be("VALID-SKU");
    }

    [Fact]
    public async Task InvokeAsync_AuthorizeChecksUser()
    {
        var request = new PermissionTestRequest { Name = "Test" };
        var context = CreateFilterContext(request, new Claim("permission", "Products.Create"));
        var nextCalled = false;

        await _filter.InvokeAsync(
            context,
            _ =>
            {
                nextCalled = true;
                return ValueTask.FromResult<object?>(Results.Ok());
            }
        );

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_AuthorizeChecksFail_WhenMissingPermission()
    {
        var request = new PermissionTestRequest { Name = "Test" };
        var context = CreateFilterContext(request);

        var result = await _filter.InvokeAsync(
            context,
            _ => ValueTask.FromResult<object?>(Results.Ok())
        );

        result.Should().BeAssignableTo<ProblemHttpResult>();
        var problem = (ProblemHttpResult)result!;
        problem.StatusCode.Should().Be(403);
    }

    private static DefaultEndpointFilterInvocationContext CreateFilterContext(
        params object[] arguments
    )
    {
        return CreateFilterContext(arguments, Array.Empty<Claim>());
    }

    private static DefaultEndpointFilterInvocationContext CreateFilterContext(
        object argument,
        params Claim[] claims
    )
    {
        return CreateFilterContext(new[] { argument }, claims);
    }

    private static DefaultEndpointFilterInvocationContext CreateFilterContext(
        object[] arguments,
        Claim[] claims
    )
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();

        if (claims.Length > 0)
        {
            var identity = new ClaimsIdentity(claims, "test");
            httpContext.User = new ClaimsPrincipal(identity);
        }

        return new DefaultEndpointFilterInvocationContext(httpContext, arguments);
    }

    #region Test FormRequest types

    [FormRequest]
    private sealed class ValidTestRequest : FormRequest<ValidTestRequest>
    {
        public string Name { get; set; } = "";
        public int Value { get; set; }

        protected override void ConfigureRules(RuleConfigurator<ValidTestRequest> rules)
        {
            rules.RuleFor(x => x.Name).NotEmpty();
            rules.RuleFor(x => x.Value).GreaterThan(0);
        }
    }

    [FormRequest]
    private sealed class UnauthorizedTestRequest : FormRequest<UnauthorizedTestRequest>
    {
        public string Name { get; set; } = "";

        public override bool Authorize(ClaimsPrincipal user) => false;

        protected override void ConfigureRules(RuleConfigurator<UnauthorizedTestRequest> rules)
        {
            rules.RuleFor(x => x.Name).NotEmpty();
        }
    }

    [FormRequest]
    private sealed class PrepareTestRequest : FormRequest<PrepareTestRequest>
    {
        public string Sku { get; set; } = "";

        public override void Prepare()
        {
            Sku = Sku.Trim().ToUpperInvariant();
        }

        protected override void ConfigureRules(RuleConfigurator<PrepareTestRequest> rules)
        {
            rules.RuleFor(x => x.Sku).NotEmpty().Matches("^[A-Z0-9-]+$");
        }
    }

    [FormRequest]
    private sealed class PermissionTestRequest : FormRequest<PermissionTestRequest>
    {
        public string Name { get; set; } = "";

        public override bool Authorize(ClaimsPrincipal user) =>
            user.Claims.Any(c => c is { Type: "permission", Value: "Products.Create" });

        protected override void ConfigureRules(RuleConfigurator<PermissionTestRequest> rules)
        {
            rules.RuleFor(x => x.Name).NotEmpty();
        }
    }

    #endregion
}
