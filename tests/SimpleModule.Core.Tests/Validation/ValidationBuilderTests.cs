using FluentAssertions;
using SimpleModule.Core.Validation;

namespace SimpleModule.Core.Tests.Validation;

public class ValidationBuilderTests
{
    [Fact]
    public void Build_WithNoErrors_ReturnsSuccess()
    {
        var result = new ValidationBuilder().Build();

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void AddErrorIf_WhenConditionTrue_AddsError()
    {
        var result = new ValidationBuilder()
            .AddErrorIf(true, "Name", "Name is required.")
            .Build();

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainKey("Name");
        result.Errors["Name"].Should().Contain("Name is required.");
    }

    [Fact]
    public void AddErrorIf_WhenConditionFalse_DoesNotAddError()
    {
        var result = new ValidationBuilder()
            .AddErrorIf(false, "Name", "Name is required.")
            .Build();

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void AddErrorIf_MultipleSameField_AccumulatesErrors()
    {
        var result = new ValidationBuilder()
            .AddErrorIf(true, "Name", "Name is required.")
            .AddErrorIf(true, "Name", "Name must be at least 2 characters.")
            .Build();

        result.Errors["Name"].Should().HaveCount(2);
    }
}
