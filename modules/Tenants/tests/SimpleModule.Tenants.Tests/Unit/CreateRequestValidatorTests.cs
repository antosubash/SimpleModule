using FluentAssertions;
using SimpleModule.Tenants.Contracts;
using SimpleModule.Tenants.Endpoints.Tenants;

namespace Tenants.Tests.Unit;

public class CreateRequestValidatorTests
{
    private readonly CreateRequestValidator _validator = new();

    [Fact]
    public void Validate_WithValidRequest_ReturnsSuccess()
    {
        var request = new CreateTenantRequest { Name = "Test", Slug = "test" };
        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyName_ReturnsError()
    {
        var request = new CreateTenantRequest { Name = "", Slug = "test" };
        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Validate_WithEmptySlug_ReturnsError()
    {
        var request = new CreateTenantRequest { Name = "Test", Slug = "" };
        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Slug");
    }

    [Fact]
    public void Validate_WithInvalidSlug_ReturnsError()
    {
        var request = new CreateTenantRequest { Name = "Test", Slug = "INVALID SLUG!" };
        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Slug");
    }

    [Fact]
    public void Validate_WithInvalidEmail_ReturnsError()
    {
        var request = new CreateTenantRequest
        {
            Name = "Test",
            Slug = "test",
            AdminEmail = "not-an-email",
        };
        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "AdminEmail");
    }
}
