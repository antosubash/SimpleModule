using FluentAssertions;
using SimpleModule.Email.Contracts;
using SimpleModule.Email.Validators;

namespace SimpleModule.Email.Tests.Unit;

public sealed class ValidatorTests
{
    [Fact]
    public void SendEmailRequestValidator_WithValidRequest_ReturnsSuccess()
    {
        var request = new SendEmailRequest
        {
            To = "test@example.com",
            Subject = "Test",
            Body = "Hello",
        };
        var result = SendEmailRequestValidator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void SendEmailRequestValidator_WithEmptyTo_ReturnsError()
    {
        var request = new SendEmailRequest
        {
            To = "",
            Subject = "Test",
            Body = "Hello",
        };
        var result = SendEmailRequestValidator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainKey("To");
    }

    [Fact]
    public void SendEmailRequestValidator_WithInvalidEmail_ReturnsError()
    {
        var request = new SendEmailRequest
        {
            To = "not-an-email",
            Subject = "Test",
            Body = "Hello",
        };
        var result = SendEmailRequestValidator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainKey("To");
    }

    [Fact]
    public void SendEmailRequestValidator_WithInvalidReplyTo_ReturnsError()
    {
        var request = new SendEmailRequest
        {
            To = "test@example.com",
            ReplyTo = "bad-email",
            Subject = "Test",
            Body = "Hello",
        };
        var result = SendEmailRequestValidator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainKey("ReplyTo");
    }

    [Fact]
    public void CreateEmailTemplateRequestValidator_WithValidRequest_ReturnsSuccess()
    {
        var request = new CreateEmailTemplateRequest
        {
            Name = "Welcome",
            Slug = "welcome-email",
            Subject = "Welcome {{name}}",
            Body = "Hello!",
        };
        var result = CreateEmailTemplateRequestValidator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateEmailTemplateRequestValidator_WithInvalidSlug_ReturnsError()
    {
        var request = new CreateEmailTemplateRequest
        {
            Name = "Welcome",
            Slug = "INVALID SLUG!",
            Subject = "Welcome",
            Body = "Hello!",
        };
        var result = CreateEmailTemplateRequestValidator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainKey("Slug");
    }

    [Fact]
    public void UpdateEmailTemplateRequestValidator_WithEmptyName_ReturnsError()
    {
        var request = new UpdateEmailTemplateRequest
        {
            Name = "",
            Subject = "Test",
            Body = "Hello!",
        };
        var result = UpdateEmailTemplateRequestValidator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainKey("Name");
    }
}
