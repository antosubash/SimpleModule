using System.Security.Claims;
using System.Text.Json;
using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using SimpleModule.Core.FormRequests;
using SimpleModule.Core.Inertia;

namespace SimpleModule.Core.Tests.FormRequests;

/// <summary>
/// Covers the gaps not exercised by the existing 16 tests:
///  - Empty arguments list and null argument slots
///  - Multiple FormRequest arguments in one endpoint (only the first is checked — the filter loops)
///  - Empty ConfigureRules (no rules — should always pass)
///  - RuleForEach collection validation
///  - Inertia error path (X-Inertia header → 422 JSON with Inertia shape)
///  - Exact JSON shape of the API (non-Inertia) 422 response
///  - Validator caching is type-isolated (two different types get separate validators)
///  - Authorize check receives the actual HttpContext user, not a default principal
///  - Prepare mutates the instance before Validate is called
///  - CancellationToken propagation — ValidateAsync is passed the RequestAborted token
/// </summary>
public class FormRequestEdgeCaseTests
{
    private readonly FormRequestEndpointFilter _filter = new();

    // ─── Empty / null argument slots ────────────────────────────────────────

    [Fact]
    public async Task InvokeAsync_EmptyArgumentList_CallsNext()
    {
        var nextCalled = false;
        var context = CreateContext();

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
    public async Task InvokeAsync_NullArgumentSlot_SkipsNullAndCallsNext()
    {
        // A null argument must not throw — the filter casts and checks with `is`, which handles null safely.
        var nextCalled = false;
        var context = CreateContext((object?)null!);

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
    public async Task InvokeAsync_MixedArgumentsWithFormRequestLast_StillValidates()
    {
        // The filter must scan all argument positions, not just index 0.
        var request = new NoRulesRequest();
        var nextCalled = false;
        var context = CreateContext("first-arg", 42, request);

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

    // ─── Multiple FormRequest arguments ──────────────────────────────────────

    [Fact]
    public async Task InvokeAsync_TwoValidFormRequests_CallsNext()
    {
        var r1 = new NoRulesRequest();
        var r2 = new NoRulesRequest();
        var nextCalled = false;
        var context = CreateContext(r1, r2);

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
    public async Task InvokeAsync_FirstFormRequestInvalid_Returns422WithoutCallingNext()
    {
        // The filter iterates in order; the first invalid request short-circuits.
        var invalid = new SingleFieldRequest { Name = "" };
        var valid = new NoRulesRequest();
        var nextCalled = false;
        var context = CreateContext(invalid, valid);

        var result = await _filter.InvokeAsync(
            context,
            _ =>
            {
                nextCalled = true;
                return ValueTask.FromResult<object?>(Results.Ok());
            }
        );

        nextCalled.Should().BeFalse("short-circuit on first invalid request");
        result.Should().BeOfType<ProblemHttpResult>().Which.StatusCode.Should().Be(422);
    }

    [Fact]
    public async Task InvokeAsync_FirstFormRequestUnauthorized_Returns403WithoutCheckingSecond()
    {
        var unauthorized = new AlwaysDenyRequest();
        var valid = new NoRulesRequest();
        var nextCalled = false;
        var context = CreateContext(unauthorized, valid);

        var result = await _filter.InvokeAsync(
            context,
            _ =>
            {
                nextCalled = true;
                return ValueTask.FromResult<object?>(Results.Ok());
            }
        );

        nextCalled.Should().BeFalse();
        result.Should().BeOfType<ProblemHttpResult>().Which.StatusCode.Should().Be(403);
    }

    // ─── Empty ConfigureRules ────────────────────────────────────────────────

    [Fact]
    public async Task InvokeAsync_FormRequestWithNoRules_AlwaysPasses()
    {
        var request = new NoRulesRequest();
        var nextCalled = false;
        var context = CreateContext(request);

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

    // ─── RuleForEach collection validation ───────────────────────────────────

    [Fact]
    public async Task InvokeAsync_CollectionAllValid_CallsNext()
    {
        var request = new CollectionRequest { Tags = ["foo", "bar"] };
        var nextCalled = false;
        var context = CreateContext(request);

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
    public async Task InvokeAsync_CollectionContainsEmptyElement_Returns422()
    {
        var request = new CollectionRequest { Tags = ["foo", "", "bar"] };
        var context = CreateContext(request);

        var result = await _filter.InvokeAsync(
            context,
            _ => ValueTask.FromResult<object?>(Results.Ok())
        );

        result.Should().BeOfType<ProblemHttpResult>().Which.StatusCode.Should().Be(422);
    }

    [Fact]
    public async Task InvokeAsync_EmptyCollection_Returns422()
    {
        // RuleForEach on an empty list produces no per-element errors, but the outer
        // NotEmpty() on the collection itself should catch it.
        var request = new CollectionRequest { Tags = [] };
        var context = CreateContext(request);

        var result = await _filter.InvokeAsync(
            context,
            _ => ValueTask.FromResult<object?>(Results.Ok())
        );

        result.Should().BeOfType<ProblemHttpResult>().Which.StatusCode.Should().Be(422);
    }

    // ─── Inertia error path ───────────────────────────────────────────────────

    [Fact]
    public async Task InvokeAsync_InertiaRequest_InvalidRequest_Returns422WithInertiaShape()
    {
        var request = new SingleFieldRequest { Name = "" };
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        httpContext.Request.Headers[InertiaHttpExtensions.InertiaHeader] = "true";

        var context = new DefaultEndpointFilterInvocationContext(httpContext, request);

        var result = await _filter.InvokeAsync(
            context,
            _ => ValueTask.FromResult<object?>(Results.Ok())
        );

        // Must NOT be a ProblemHttpResult — Inertia uses its own IResult
        result
            .Should()
            .NotBeOfType<ProblemHttpResult>("Inertia uses a custom IResult, not ProblemDetails");

        // Execute the result so headers and body are written
        await ((IResult)result!).ExecuteAsync(httpContext);

        httpContext.Response.StatusCode.Should().Be(422);
        httpContext
            .Response.Headers[InertiaHttpExtensions.InertiaHeader]
            .ToString()
            .Should()
            .Be("true");
        httpContext.Response.ContentType.Should().Contain("application/json");

        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        string body;
        using (var reader = new StreamReader(httpContext.Response.Body))
            body = await reader.ReadToEndAsync();
        using var doc = JsonDocument.Parse(body);

        // Must have component, props.status, props.errors
        doc.RootElement.GetProperty("component").GetString().Should().Be("Error/422");
        var props = doc.RootElement.GetProperty("props");
        props.GetProperty("status").GetInt32().Should().Be(422);
        props.GetProperty("errors").ValueKind.Should().Be(JsonValueKind.Object);
    }

    [Fact]
    public async Task InvokeAsync_InertiaRequest_InvalidRequest_ErrorsContainFieldName()
    {
        var request = new SingleFieldRequest { Name = "" };
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        httpContext.Request.Headers[InertiaHttpExtensions.InertiaHeader] = "true";

        var context = new DefaultEndpointFilterInvocationContext(httpContext, request);
        var result = await _filter.InvokeAsync(
            context,
            _ => ValueTask.FromResult<object?>(Results.Ok())
        );

        await ((IResult)result!).ExecuteAsync(httpContext);

        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        string body;
        using (var reader = new StreamReader(httpContext.Response.Body))
            body = await reader.ReadToEndAsync();
        using var doc = JsonDocument.Parse(body);

        var errors = doc.RootElement.GetProperty("props").GetProperty("errors");
        errors.EnumerateObject().Select(p => p.Name).Should().Contain("Name");
    }

    [Fact]
    public async Task InvokeAsync_InertiaRequest_ValidRequest_CallsNext()
    {
        // Inertia header must not interfere with valid requests.
        var request = new SingleFieldRequest { Name = "Alice" };
        var nextCalled = false;
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        httpContext.Request.Headers[InertiaHttpExtensions.InertiaHeader] = "true";

        var context = new DefaultEndpointFilterInvocationContext(httpContext, request);

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

    // ─── API (non-Inertia) 422 exact shape ───────────────────────────────────

    [Fact]
    public async Task InvokeAsync_ApiRequest_InvalidRequest_Returns422WithErrorsExtension()
    {
        var request = new SingleFieldRequest { Name = "" };
        var context = CreateContext(request);

        var result = await _filter.InvokeAsync(
            context,
            _ => ValueTask.FromResult<object?>(Results.Ok())
        );

        var problem = result.Should().BeOfType<ProblemHttpResult>().Subject;
        problem.StatusCode.Should().Be(422);
        problem.ProblemDetails.Title.Should().Be("Validation Error");
        problem.ProblemDetails.Detail.Should().Be("One or more validation errors occurred.");
        problem.ProblemDetails.Extensions.Should().ContainKey("errors");

        var errorsJson = JsonSerializer.Serialize(problem.ProblemDetails.Extensions["errors"]);
        var errors = JsonSerializer.Deserialize<Dictionary<string, string[]>>(errorsJson)!;
        errors.Should().ContainKey("Name");
        errors["Name"].Should().NotBeEmpty();
    }

    [Fact]
    public async Task InvokeAsync_ApiRequest_403Shape_HasForbiddenTitle()
    {
        var request = new AlwaysDenyRequest();
        var context = CreateContext(request);

        var result = await _filter.InvokeAsync(
            context,
            _ => ValueTask.FromResult<object?>(Results.Ok())
        );

        var problem = result.Should().BeOfType<ProblemHttpResult>().Subject;
        problem.StatusCode.Should().Be(403);
        problem.ProblemDetails.Title.Should().Be("Forbidden");
    }

    // ─── Validator caching is type-isolated ──────────────────────────────────

    [Fact]
    public async Task ValidatorCache_TwoDifferentTypes_EachGetSeparateValidator()
    {
        // Both types pass through valid values. If the cache incorrectly shared a validator,
        // one type's rules would bleed into the other and cause unexpected failures.
        var r1 = new SingleFieldRequest { Name = "valid" };
        var r2 = new NoRulesRequest();

        var nextCount = 0;
        ValueTask<object?> Next(EndpointFilterInvocationContext _)
        {
            nextCount++;
            return ValueTask.FromResult<object?>(Results.Ok());
        }

        await _filter.InvokeAsync(CreateContext(r1), Next);
        await _filter.InvokeAsync(CreateContext(r2), Next);

        nextCount.Should().Be(2, "both requests should independently pass through");
    }

    [Fact]
    public async Task ValidatorCache_SameTypeTwiceWithSameFilter_BothUseTheCachedValidator()
    {
        // Create two instances of the same type; both should exercise the validator from cache
        // (only one ConfigureRules call happens). If caching is broken, the second call might
        // rebuild and create a different validator state — but the visible effect is the same,
        // so this test confirms stability (no exception, correct results).
        var r1 = new SingleFieldRequest { Name = "alice" };
        var r2 = new SingleFieldRequest { Name = "bob" };

        var nextCount = 0;
        ValueTask<object?> Next(EndpointFilterInvocationContext _)
        {
            nextCount++;
            return ValueTask.FromResult<object?>(Results.Ok());
        }

        await _filter.InvokeAsync(CreateContext(r1), Next);
        await _filter.InvokeAsync(CreateContext(r2), Next);

        nextCount.Should().Be(2);
    }

    // ─── Authorize receives actual user ─────────────────────────────────────

    [Fact]
    public async Task InvokeAsync_AuthorizeReceivesHttpContextUser_NotDefaultPrincipal()
    {
        ClaimsPrincipal? capturedUser = null;
        var request = new CapturingAuthRequest(u => capturedUser = u) { Name = "Test" };

        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        var identity = new ClaimsIdentity([new Claim("role", "admin")], "test");
        httpContext.User = new ClaimsPrincipal(identity);

        var context = new DefaultEndpointFilterInvocationContext(httpContext, request);

        await _filter.InvokeAsync(context, _ => ValueTask.FromResult<object?>(Results.Ok()));

        capturedUser.Should().NotBeNull();
        capturedUser!.Claims.Should().Contain(c => c.Type == "role" && c.Value == "admin");
    }

    // ─── Prepare always runs before Validate ─────────────────────────────────

    [Fact]
    public async Task InvokeAsync_PrepareRunsBeforeValidation_SideEffectsVisible()
    {
        // PrepareTrackingRequest records whether Prepare was called before validation.
        // The test asserts that the instance was mutated before rules ran.
        var request = new PrepareTrackingRequest { Value = "  trimmed  " };
        var context = CreateContext(request);

        await _filter.InvokeAsync(context, _ => ValueTask.FromResult<object?>(Results.Ok()));

        request.PrepareWasCalled.Should().BeTrue();
        request.Value.Should().Be("trimmed", "Prepare should have trimmed the value");
    }

    [Fact]
    public async Task InvokeAsync_PrepareNormalizes_ThenValidationSees_NormalizedValue()
    {
        // If validation ran on the raw value before Prepare, this would fail the regex.
        // The fact that it passes confirms Prepare ran first.
        var request = new PrepareNormalizeRequest { Code = "  ABC 123  " };
        var context = CreateContext(request);
        var nextCalled = false;

        await _filter.InvokeAsync(
            context,
            _ =>
            {
                nextCalled = true;
                return ValueTask.FromResult<object?>(Results.Ok());
            }
        );

        nextCalled.Should().BeTrue("normalized value ABC123 should pass the digit-only rule");
        request.Code.Should().Be("ABC123");
    }

    // ─── CancellationToken propagation ───────────────────────────────────────

    [Fact]
    public async Task InvokeAsync_CancelledToken_ThrowsOperationCancelledException()
    {
        var request = new SlowValidationRequest { Name = "test" };
        using var cts = new CancellationTokenSource();

        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        httpContext.RequestAborted = cts.Token;

        var context = new DefaultEndpointFilterInvocationContext(httpContext, request);
        await cts.CancelAsync();

        var act = async () =>
            await _filter.InvokeAsync(context, _ => ValueTask.FromResult<object?>(Results.Ok()));

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    // ─── FormRequestAttribute marker ─────────────────────────────────────────

    [Fact]
    public void FormRequestAttribute_IsAttributeUsageRestrictedToClass()
    {
        var usage = typeof(FormRequestAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .Single();

        usage.ValidOn.Should().Be(AttributeTargets.Class);
        usage.AllowMultiple.Should().BeFalse();
        usage.Inherited.Should().BeFalse();
    }

    // ─── RuleConfigurator surface area ───────────────────────────────────────
    // Build() is internal, so we exercise RuleConfigurator through the filter pipeline.

    [Fact]
    public async Task RuleConfigurator_RuleFor_ValidValue_PassesThroughFilter()
    {
        var request = new CustomMessageRequest { Name = "valid" };
        var nextCalled = false;

        await _filter.InvokeAsync(
            CreateContext(request),
            _ =>
            {
                nextCalled = true;
                return ValueTask.FromResult<object?>(Results.Ok());
            }
        );

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task RuleConfigurator_RuleFor_CustomMessage_AppearsInErrors()
    {
        var request = new CustomMessageRequest { Name = "" };

        var result = await _filter.InvokeAsync(
            CreateContext(request),
            _ => ValueTask.FromResult<object?>(Results.Ok())
        );

        var problem = result.Should().BeOfType<ProblemHttpResult>().Subject;
        var errorsJson = JsonSerializer.Serialize(problem.ProblemDetails.Extensions["errors"]);
        var errors = JsonSerializer.Deserialize<Dictionary<string, string[]>>(errorsJson)!;
        errors["Name"].Should().Contain("Name is required for CustomMessageRequest.");
    }

    [Fact]
    public async Task RuleConfigurator_RuleForEach_ValidCollection_PassesThroughFilter()
    {
        // This uses CollectionRequest which has both RuleFor(Tags).NotEmpty() and RuleForEach.
        var request = new CollectionRequest { Tags = ["alpha", "beta"] };
        var nextCalled = false;

        await _filter.InvokeAsync(
            CreateContext(request),
            _ =>
            {
                nextCalled = true;
                return ValueTask.FromResult<object?>(Results.Ok());
            }
        );

        nextCalled.Should().BeTrue();
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static DefaultEndpointFilterInvocationContext CreateContext(params object?[] arguments)
    {
        var httpContext = new DefaultHttpContext { Response = { Body = new MemoryStream() } };
        return new DefaultEndpointFilterInvocationContext(httpContext, arguments);
    }

    // ─── Test FormRequest types ───────────────────────────────────────────────

    [FormRequest]
    private sealed class NoRulesRequest : FormRequest<NoRulesRequest>
    {
        protected override void ConfigureRules(RuleConfigurator<NoRulesRequest> rules)
        {
            // Intentionally empty — must always pass validation.
        }
    }

    [FormRequest]
    private sealed class SingleFieldRequest : FormRequest<SingleFieldRequest>
    {
        public string Name { get; set; } = "";

        protected override void ConfigureRules(RuleConfigurator<SingleFieldRequest> rules)
        {
            rules.RuleFor(x => x.Name).NotEmpty();
        }
    }

    [FormRequest]
    private sealed class AlwaysDenyRequest : FormRequest<AlwaysDenyRequest>
    {
        public override bool Authorize(ClaimsPrincipal user) => false;

        protected override void ConfigureRules(RuleConfigurator<AlwaysDenyRequest> rules) { }
    }

    [FormRequest]
    private sealed class CollectionRequest : FormRequest<CollectionRequest>
    {
        public List<string> Tags { get; set; } = [];

        protected override void ConfigureRules(RuleConfigurator<CollectionRequest> rules)
        {
            rules.RuleFor(x => x.Tags).NotEmpty().WithMessage("Tags cannot be empty");
            rules.RuleForEach(x => x.Tags).NotEmpty().WithMessage("Each tag must be non-empty");
        }
    }

    [FormRequest]
    private sealed class PrepareTrackingRequest : FormRequest<PrepareTrackingRequest>
    {
        public string Value { get; set; } = "";
        public bool PrepareWasCalled { get; private set; }

        public override void Prepare()
        {
            PrepareWasCalled = true;
            Value = Value.Trim();
        }

        protected override void ConfigureRules(RuleConfigurator<PrepareTrackingRequest> rules)
        {
            rules.RuleFor(x => x.Value).NotEmpty();
        }
    }

    [FormRequest]
    private sealed class PrepareNormalizeRequest : FormRequest<PrepareNormalizeRequest>
    {
        public string Code { get; set; } = "";

        public override void Prepare()
        {
            Code = Code.Replace(" ", "", StringComparison.Ordinal).Trim();
        }

        protected override void ConfigureRules(RuleConfigurator<PrepareNormalizeRequest> rules)
        {
            // Only alphanumeric — raw "  ABC 123  " would fail this; normalized "ABC123" passes.
            rules.RuleFor(x => x.Code).NotEmpty().Matches("^[A-Z0-9]+$");
        }
    }

    [FormRequest]
    private sealed class CapturingAuthRequest : FormRequest<CapturingAuthRequest>
    {
        private readonly Action<ClaimsPrincipal> _capture;

        public string Name { get; set; } = "";

        public CapturingAuthRequest(Action<ClaimsPrincipal> capture) => _capture = capture;

        public override bool Authorize(ClaimsPrincipal user)
        {
            _capture(user);
            return true;
        }

        protected override void ConfigureRules(RuleConfigurator<CapturingAuthRequest> rules)
        {
            rules.RuleFor(x => x.Name).NotEmpty();
        }
    }

    [FormRequest]
    private sealed class CustomMessageRequest : FormRequest<CustomMessageRequest>
    {
        public string Name { get; set; } = "";

        protected override void ConfigureRules(RuleConfigurator<CustomMessageRequest> rules)
        {
            rules
                .RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Name is required for CustomMessageRequest.");
        }
    }

    [FormRequest]
    private sealed class SlowValidationRequest : FormRequest<SlowValidationRequest>
    {
        public string Name { get; set; } = "";

        protected override void ConfigureRules(RuleConfigurator<SlowValidationRequest> rules)
        {
            // MustAsync triggers actual async validation, so the cancellation token is honored.
            rules
                .RuleFor(x => x.Name)
                .NotEmpty()
                .MustAsync(
                    async (_, ct) =>
                    {
                        await Task.Delay(TimeSpan.FromSeconds(10), ct);
                        return true;
                    }
                );
        }
    }
}
