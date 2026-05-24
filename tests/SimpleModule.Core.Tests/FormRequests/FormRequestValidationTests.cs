using System.Security.Claims;
using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using SimpleModule.Core.FormRequests;

namespace SimpleModule.Core.Tests.FormRequests;

public class FormRequestValidationTests
{
    private readonly FormRequestEndpointFilter _filter = new();

    [Fact]
    public async Task ValidRequest_PassesThrough()
    {
        var request = new TestFormRequest { Name = "Valid", Price = 10m };
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
    public async Task EmptyName_Returns422()
    {
        var request = new TestFormRequest { Name = "", Price = 10m };

        var result = await _filter.InvokeAsync(
            CreateContext(request),
            _ => ValueTask.FromResult<object?>(Results.Ok())
        );

        var problem = result.Should().BeOfType<ProblemHttpResult>().Subject;
        problem.StatusCode.Should().Be(422);
    }

    [Fact]
    public async Task NegativePrice_Returns422()
    {
        var request = new TestFormRequest { Name = "Valid", Price = -5m };

        var result = await _filter.InvokeAsync(
            CreateContext(request),
            _ => ValueTask.FromResult<object?>(Results.Ok())
        );

        result.Should().BeOfType<ProblemHttpResult>();
    }

    [Fact]
    public async Task MultipleViolations_ReturnsAllErrors()
    {
        var request = new TestFormRequest { Name = "", Price = -5m };

        var result = await _filter.InvokeAsync(
            CreateContext(request),
            _ => ValueTask.FromResult<object?>(Results.Ok())
        );

        var problem = (ProblemHttpResult)result!;
        problem.ProblemDetails.Extensions.Should().ContainKey("errors");
    }

    [Fact]
    public async Task ValidatorIsCached_AcrossInstances()
    {
        var r1 = new TestFormRequest { Name = "A", Price = 1m };
        var r2 = new TestFormRequest { Name = "B", Price = 2m };

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

    [Fact]
    public void Prepare_DefaultImplementation_DoesNotThrow()
    {
        var request = new TestFormRequest { Name = "Test", Price = 1m };
        var act = () => request.Prepare();
        act.Should().NotThrow();
    }

    [Fact]
    public void Authorize_DefaultImplementation_ReturnsTrue()
    {
        var request = new TestFormRequest { Name = "Test", Price = 1m };
        request.Authorize(new ClaimsPrincipal()).Should().BeTrue();
    }

    private static DefaultEndpointFilterInvocationContext CreateContext(FormRequest request)
    {
        var httpContext = new DefaultHttpContext { Response = { Body = new MemoryStream() } };
        return new DefaultEndpointFilterInvocationContext(httpContext, request);
    }

    [FormRequest]
    private sealed class TestFormRequest : FormRequest<TestFormRequest>
    {
        public string Name { get; set; } = "";
        public decimal Price { get; set; }

        protected override void ConfigureRules(RuleConfigurator<TestFormRequest> rules)
        {
            rules.RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            rules.RuleFor(x => x.Price).GreaterThan(0);
        }
    }
}
