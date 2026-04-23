using System.Reflection;
using FluentAssertions;
using SimpleModule.Cli.Commands.Version;

namespace SimpleModule.Cli.Tests;

public sealed class VersionCommandTests
{
    private static string InvokeResolveVersion()
    {
        var method = typeof(VersionCommand).GetMethod(
            "ResolveVersion",
            BindingFlags.NonPublic | BindingFlags.Static
        )!;
        return (string)method.Invoke(null, null)!;
    }

    [Fact]
    public void ResolveVersion_ReturnsNonEmptyString()
    {
        InvokeResolveVersion().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void ResolveVersion_StripsBuildMetadataSuffix()
    {
        InvokeResolveVersion().Should().NotContain("+");
    }
}
