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

    [Fact]
    public void AddErrorIf_MultipleFields_AccumulatesErrorsSeparately()
    {
        var result = new ValidationBuilder()
            .AddErrorIf(true, "Name", "Name is required.")
            .AddErrorIf(true, "Price", "Price must be positive.")
            .Build();

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainKey("Name");
        result.Errors.Should().ContainKey("Price");
        result.Errors["Name"].Should().HaveCount(1);
        result.Errors["Price"].Should().HaveCount(1);
    }

    [Fact]
    public void Build_CalledTwice_ReturnsSameResult()
    {
        var builder = new ValidationBuilder()
            .AddErrorIf(true, "Name", "Name is required.");

        var result1 = builder.Build();
        var result2 = builder.Build();

        result1.IsValid.Should().Be(result2.IsValid);
        result1.Errors.Should().BeEquivalentTo(result2.Errors);
    }

    [Fact]
    public void AddErrorIf_MixedConditions_OnlyAddsForTrue()
    {
        var result = new ValidationBuilder()
            .AddErrorIf(true, "Name", "Error one.")
            .AddErrorIf(false, "Name", "Error two.")
            .AddErrorIf(true, "Name", "Error three.")
            .Build();

        result.IsValid.Should().BeFalse();
        result.Errors["Name"].Should().HaveCount(2);
        result.Errors["Name"].Should().Contain("Error one.");
        result.Errors["Name"].Should().Contain("Error three.");
        result.Errors["Name"].Should().NotContain("Error two.");
    }
}
