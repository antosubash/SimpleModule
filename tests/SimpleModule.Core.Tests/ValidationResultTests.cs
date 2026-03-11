using FluentAssertions;
using SimpleModule.Core.Validation;

namespace SimpleModule.Core.Tests;

public class ValidationResultTests
{
    [Fact]
    public void Success_IsValid_WithNoErrors()
    {
        var result = ValidationResult.Success;

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Success_IsSingleton()
    {
        var a = ValidationResult.Success;
        var b = ValidationResult.Success;

        a.Should().BeSameAs(b);
    }

    [Fact]
    public void WithErrors_IsNotValid_ContainsErrors()
    {
        var errors = new Dictionary<string, string[]>
        {
            ["Field"] = ["Error message"],
        };

        var result = ValidationResult.WithErrors(errors);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainKey("Field");
        result.Errors["Field"].Should().ContainSingle("Error message");
    }

    [Fact]
    public void WithErrors_MultipleFields_AllPresent()
    {
        var errors = new Dictionary<string, string[]>
        {
            ["Name"] = ["Required"],
            ["Age"] = ["Must be positive", "Must be under 200"],
        };

        var result = ValidationResult.WithErrors(errors);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(2);
        result.Errors["Age"].Should().HaveCount(2);
    }

    [Fact]
    public void WithErrors_EmptyDictionary_IsNotValid()
    {
        var result = ValidationResult.WithErrors(new Dictionary<string, string[]>());

        result.IsValid.Should().BeFalse();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void RecordEquality_SameValues_AreEqual()
    {
        var errors = new Dictionary<string, string[]>();
        var a = new ValidationResult(true, errors);
        var b = new ValidationResult(true, errors);

        a.Should().Be(b);
    }
}
