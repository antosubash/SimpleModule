using System.Reflection;
using FluentAssertions;
using SimpleModule.Cli.Commands.Doctor;
using SimpleModule.Cli.Commands.Doctor.Checks;

namespace SimpleModule.Cli.Tests;

public sealed class DoctorCommandTests
{
    private static int InvokeStatusPriority(CheckStatus status)
    {
        var method = typeof(DoctorCommand).GetMethod(
            "StatusPriority",
            BindingFlags.NonPublic | BindingFlags.Static
        )!;
        return (int)method.Invoke(null, [status])!;
    }

    [Fact]
    public void StatusPriority_OrdersFailBeforeWarnBeforePass()
    {
        var fail = InvokeStatusPriority(CheckStatus.Fail);
        var warn = InvokeStatusPriority(CheckStatus.Warning);
        var pass = InvokeStatusPriority(CheckStatus.Pass);

        fail.Should().BeLessThan(warn);
        warn.Should().BeLessThan(pass);
    }
}
