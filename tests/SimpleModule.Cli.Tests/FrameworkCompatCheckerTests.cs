using FluentAssertions;
using SimpleModule.Cli.Infrastructure;

namespace SimpleModule.Cli.Tests;

public class FrameworkCompatCheckerTests
{
    [Theory]
    [InlineData(">=0.0.38 <1.0.0", "0.0.38", true)]
    [InlineData(">=0.0.38 <1.0.0", "0.0.99", true)]
    [InlineData(">=0.0.38 <1.0.0", "0.0.37", false)]
    [InlineData(">=0.0.38 <1.0.0", "1.0.0", false)]
    [InlineData(">=0.0.38 <1.0.0", "1.2.3", false)]
    [InlineData(">=1.0.0", "1.0.0", true)]
    [InlineData(">=1.0.0", "0.9.9", false)]
    [InlineData(">=1.0.0", "99.0.0", true)]
    public void IsCompatible_EvaluatesRanges(string range, string version, bool expected)
    {
        var result = FrameworkCompatChecker.Check(range, version);

        result.Compatible.Should().Be(expected);
    }

    [Theory]
    [InlineData(">=0.0.39-local <1.0.0", "0.0.39-local", true)]
    [InlineData(">=0.0.39 <1.0.0", "0.0.39-local", false)] // prerelease < release
    [InlineData(">=0.0.39-alpha <1.0.0", "0.0.39", true)] // release > prerelease
    public void IsCompatible_HandlesPrereleaseOrdering(string range, string version, bool expected)
    {
        FrameworkCompatChecker.Check(range, version).Compatible.Should().Be(expected);
    }

    [Fact]
    public void EmptyRange_IsCompatibleWithWarning()
    {
        var result = FrameworkCompatChecker.Check("", "1.0.0");

        result.Compatible.Should().BeTrue();
        result.Reason.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void UnparseableRange_IsIncompatibleWithReason()
    {
        var result = FrameworkCompatChecker.Check("banana", "1.0.0");

        result.Compatible.Should().BeFalse();
        result.Reason.Should().Contain("banana");
    }

    [Fact]
    public void UnparseableHostVersion_IsIncompatibleWithReason()
    {
        var result = FrameworkCompatChecker.Check(">=1.0.0", "not-a-version");

        result.Compatible.Should().BeFalse();
        result.Reason.Should().Contain("not-a-version");
    }

    [Fact]
    public void NumericSegments_CompareNumericallyNotLexically()
    {
        FrameworkCompatChecker.Check(">=0.0.9 <1.0.0", "0.0.10").Compatible.Should().BeTrue();
    }
}
