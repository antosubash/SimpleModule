using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.FormRequests;
using SimpleModule.Email.FormRequests;

namespace SimpleModule.Email.Tests.Unit;

/// <summary>
/// Covers CreateTemplateFormRequest in isolation — authorization, normalization (Prepare),
/// and each validation rule. The filter pipeline is exercised via FormRequestEndpointFilter
/// directly so we do not need a running server.
/// </summary>
public class CreateTemplateFormRequestTests
{
    private readonly FormRequestEndpointFilter _filter = new();

    // ─── Authorize ────────────────────────────────────────────────────────────

    [Fact]
    public void Authorize_UserWithManageTemplatesPermission_ReturnsTrue()
    {
        var user = MakeUser(EmailPermissions.ManageTemplates);
        var request = ValidRequest();

        request.Authorize(user).Should().BeTrue();
    }

    [Fact]
    public void Authorize_UserWithoutPermission_ReturnsFalse()
    {
        var user = MakeUser("SomeOtherPermission");
        var request = ValidRequest();

        request.Authorize(user).Should().BeFalse();
    }

    [Fact]
    public void Authorize_AnonymousUser_ReturnsFalse()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity());
        var request = ValidRequest();

        request.Authorize(user).Should().BeFalse();
    }

    [Fact]
    public async Task Filter_UnauthorizedUser_Returns403()
    {
        var request = ValidRequest();
        var context = CreateContext(request); // no permission claims

        var result = await _filter.InvokeAsync(
            context,
            _ => ValueTask.FromResult<object?>(Results.Ok())
        );

        result.Should().BeOfType<ProblemHttpResult>().Which.StatusCode.Should().Be(403);
    }

    // ─── Prepare / normalization ──────────────────────────────────────────────

    [Fact]
    public void Prepare_TrimsNameAndSubject()
    {
        var request = new CreateTemplateFormRequest
        {
            Name = "  Welcome Email  ",
            Slug = "welcome-email",
            Subject = "  Hello {{name}}  ",
            Body = "Hi there",
        };

        request.Prepare();

        request.Name.Should().Be("Welcome Email");
        request.Subject.Should().Be("Hello {{name}}");
    }

    [Fact]
    public void Prepare_NormalizesSlugToLowercase()
    {
        var request = new CreateTemplateFormRequest
        {
            Name = "Test",
            Slug = "  WELCOME-Email  ",
            Subject = "Test",
            Body = "Body",
        };

        request.Prepare();

        request.Slug.Should().Be("welcome-email");
    }

    [Fact]
    public void Prepare_SlugTrimmedAndLowercased()
    {
        var request = new CreateTemplateFormRequest
        {
            Slug = "   Order-Confirmation   ",
            Name = "n",
            Subject = "s",
            Body = "b",
        };

        request.Prepare();

        request.Slug.Should().Be("order-confirmation");
    }

    // ─── Validation rules ─────────────────────────────────────────────────────

    [Fact]
    public async Task ValidRequest_PassesValidation()
    {
        var request = ValidRequest();
        var nextCalled = false;
        var context = CreateContext(request, hasPermission: true);

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
    public async Task EmptyName_Returns422()
    {
        var request = ValidRequest();
        request.Name = "";
        var context = CreateContext(request, hasPermission: true);

        var result = await _filter.InvokeAsync(
            context,
            _ => ValueTask.FromResult<object?>(Results.Ok())
        );

        AssertFieldError(result, "Name");
    }

    [Fact]
    public async Task NameExceeds200Characters_Returns422()
    {
        var request = ValidRequest();
        request.Name = new string('A', 201);
        var context = CreateContext(request, hasPermission: true);

        var result = await _filter.InvokeAsync(
            context,
            _ => ValueTask.FromResult<object?>(Results.Ok())
        );

        AssertFieldError(result, "Name");
    }

    [Fact]
    public async Task Exactly200CharactersName_Passes()
    {
        var request = ValidRequest();
        request.Name = new string('A', 200);
        var nextCalled = false;
        var context = CreateContext(request, hasPermission: true);

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
    public async Task EmptySlug_Returns422()
    {
        var request = ValidRequest();
        request.Slug = "";
        var context = CreateContext(request, hasPermission: true);

        var result = await _filter.InvokeAsync(
            context,
            _ => ValueTask.FromResult<object?>(Results.Ok())
        );

        AssertFieldError(result, "Slug");
    }

    [Theory]
    // These values remain invalid even after Prepare() lowercases and trims them:
    [InlineData("has space")] // spaces survive lowercase (become "has space")
    [InlineData("trailing-")] // trailing hyphen
    [InlineData("-leading")] // leading hyphen
    [InlineData("double--hyphen")] // consecutive hyphens
    [InlineData("special!char")] // special characters
    public async Task InvalidSlugPattern_Returns422(string slug)
    {
        var request = ValidRequest();
        request.Slug = slug;
        var context = CreateContext(request, hasPermission: true);

        var result = await _filter.InvokeAsync(
            context,
            _ => ValueTask.FromResult<object?>(Results.Ok())
        );

        AssertFieldError(result, "Slug");
    }

    [Theory]
    [InlineData("welcome")]
    [InlineData("order-confirmation")]
    [InlineData("a1b2c3")]
    [InlineData("x")]
    public async Task ValidSlugPattern_Passes(string slug)
    {
        var request = ValidRequest();
        request.Slug = slug;
        var nextCalled = false;
        var context = CreateContext(request, hasPermission: true);

        await _filter.InvokeAsync(
            context,
            _ =>
            {
                nextCalled = true;
                return ValueTask.FromResult<object?>(Results.Ok());
            }
        );

        nextCalled.Should().BeTrue($"slug '{slug}' is valid");
    }

    [Fact]
    public async Task EmptySubject_Returns422()
    {
        var request = ValidRequest();
        request.Subject = "";
        var context = CreateContext(request, hasPermission: true);

        var result = await _filter.InvokeAsync(
            context,
            _ => ValueTask.FromResult<object?>(Results.Ok())
        );

        AssertFieldError(result, "Subject");
    }

    [Fact]
    public async Task EmptyBody_Returns422()
    {
        var request = ValidRequest();
        request.Body = "";
        var context = CreateContext(request, hasPermission: true);

        var result = await _filter.InvokeAsync(
            context,
            _ => ValueTask.FromResult<object?>(Results.Ok())
        );

        AssertFieldError(result, "Body");
    }

    [Fact]
    public async Task ValidEmailReplyTo_Passes()
    {
        var request = ValidRequest();
        request.DefaultReplyTo = "support@example.com";
        var nextCalled = false;
        var context = CreateContext(request, hasPermission: true);

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
    public async Task InvalidEmailReplyTo_Returns422()
    {
        var request = ValidRequest();
        request.DefaultReplyTo = "not-an-email";
        var context = CreateContext(request, hasPermission: true);

        var result = await _filter.InvokeAsync(
            context,
            _ => ValueTask.FromResult<object?>(Results.Ok())
        );

        AssertFieldError(result, "DefaultReplyTo");
    }

    [Fact]
    public async Task NullReplyTo_Passes()
    {
        var request = ValidRequest();
        request.DefaultReplyTo = null;
        var nextCalled = false;
        var context = CreateContext(request, hasPermission: true);

        await _filter.InvokeAsync(
            context,
            _ =>
            {
                nextCalled = true;
                return ValueTask.FromResult<object?>(Results.Ok());
            }
        );

        nextCalled.Should().BeTrue("null DefaultReplyTo is allowed");
    }

    [Fact]
    public async Task WhitespaceReplyTo_PassesBecauseWhenConditionSkipsIt()
    {
        // The rule uses `.When(x => !string.IsNullOrWhiteSpace(x.DefaultReplyTo))`,
        // so a whitespace-only value must not trigger the email format check.
        var request = ValidRequest();
        request.DefaultReplyTo = "   ";
        var nextCalled = false;
        var context = CreateContext(request, hasPermission: true);

        await _filter.InvokeAsync(
            context,
            _ =>
            {
                nextCalled = true;
                return ValueTask.FromResult<object?>(Results.Ok());
            }
        );

        nextCalled.Should().BeTrue("whitespace DefaultReplyTo is skipped by When condition");
    }

    // ─── Prepare + Validate integration ──────────────────────────────────────

    [Fact]
    public async Task PrepareNormalizesSlugThenValidationSees_LowercaseSlug()
    {
        // After Prepare(), "Welcome-Email" becomes "welcome-email" which matches the slug pattern.
        // If validation ran before Prepare, the uppercase letters would fail the regex.
        var request = new CreateTemplateFormRequest
        {
            Name = "Welcome",
            Slug = "Welcome-Email", // valid after normalization
            Subject = "Hello",
            Body = "Body text",
        };
        var nextCalled = false;
        var context = CreateContext(request, hasPermission: true);

        await _filter.InvokeAsync(
            context,
            _ =>
            {
                nextCalled = true;
                return ValueTask.FromResult<object?>(Results.Ok());
            }
        );

        nextCalled.Should().BeTrue("Prepare must run before validation");
        request.Slug.Should().Be("welcome-email");
    }

    // ─── Mapping to DTO (CreateTemplateEndpoint contract) ────────────────────

    [Fact]
    public void FormRequest_MapsAllFieldsToDto()
    {
        // The CreateTemplateEndpoint maps FormRequest → CreateEmailTemplateRequest.
        // Verify that all fields the endpoint touches are accessible on the request.
        var request = new CreateTemplateFormRequest
        {
            Name = "Invoice",
            Slug = "invoice",
            Subject = "Your invoice",
            Body = "<p>Hello</p>",
            IsHtml = true,
            DefaultReplyTo = "billing@example.com",
        };

        // If any property was renamed or removed, this would fail to compile.
        _ = request.Name;
        _ = request.Slug;
        _ = request.Subject;
        _ = request.Body;
        _ = request.IsHtml;
        _ = request.DefaultReplyTo;

        request.Name.Should().Be("Invoice");
        request.Slug.Should().Be("invoice");
        request.Subject.Should().Be("Your invoice");
        request.Body.Should().Be("<p>Hello</p>");
        request.IsHtml.Should().BeTrue();
        request.DefaultReplyTo.Should().Be("billing@example.com");
    }

    [Fact]
    public void FormRequest_IsHtml_DefaultsToTrue()
    {
        var request = new CreateTemplateFormRequest();
        request.IsHtml.Should().BeTrue();
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static CreateTemplateFormRequest ValidRequest() =>
        new()
        {
            Name = "Welcome",
            Slug = "welcome",
            Subject = "Hello {{name}}",
            Body = "<p>Welcome to the platform!</p>",
            IsHtml = true,
        };

    private static ClaimsPrincipal MakeUser(string permission)
    {
        var claims = new[] { new Claim("permission", permission) };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }

    private static DefaultEndpointFilterInvocationContext CreateContext(
        CreateTemplateFormRequest request,
        bool hasPermission = false
    )
    {
        var httpContext = new DefaultHttpContext { Response = { Body = new MemoryStream() } };

        if (hasPermission)
        {
            var identity = new ClaimsIdentity(
                [new Claim("permission", EmailPermissions.ManageTemplates)],
                "test"
            );
            httpContext.User = new ClaimsPrincipal(identity);
        }

        return new DefaultEndpointFilterInvocationContext(httpContext, request);
    }

    private static void AssertFieldError(object? result, string fieldName)
    {
        var problem = result.Should().BeOfType<ProblemHttpResult>().Subject;
        problem.StatusCode.Should().Be(422);
        var errorsJson = System.Text.Json.JsonSerializer.Serialize(
            problem.ProblemDetails.Extensions["errors"]
        );
        var errors = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string[]>>(
            errorsJson
        )!;
        errors
            .Should()
            .ContainKey(fieldName, $"field '{fieldName}' should have a validation error");
    }
}
