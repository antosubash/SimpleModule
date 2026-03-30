using FluentAssertions;
using SimpleModule.Tenants.Contracts;
using SimpleModule.Tenants.Endpoints.Tenants;

namespace Tenants.Tests.Unit;

public class CreateRequestValidatorTests
{
    [Fact]
    public void Validate_WithValidRequest_ReturnsSuccess()
    {
        var request = new CreateTenantRequest { Name = "Test", Slug = "test" };
        var result = CreateRequestValidator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyName_ReturnsError()
    {
        var request = new CreateTenantRequest { Name = "", Slug = "test" };
        var result = CreateRequestValidator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainKey("Name");
    }

    [Fact]
    public void Validate_WithEmptySlug_ReturnsError()
    {
        var request = new CreateTenantRequest { Name = "Test", Slug = "" };
        var result = CreateRequestValidator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainKey("Slug");
    }

    [Fact]
    public void Validate_WithInvalidSlug_ReturnsError()
    {
        var request = new CreateTenantRequest { Name = "Test", Slug = "INVALID SLUG!" };
        var result = CreateRequestValidator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainKey("Slug");
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
        var result = CreateRequestValidator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainKey("AdminEmail");
    }
}
