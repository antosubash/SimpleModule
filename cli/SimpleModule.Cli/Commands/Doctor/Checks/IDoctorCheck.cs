using NetEscapades.EnumGenerators;

namespace SimpleModule.Cli.Commands.Doctor.Checks;

[EnumExtensions]
public enum CheckStatus
{
    Pass,
    Warning,
    Fail,
}

public sealed record CheckResult(string Name, CheckStatus Status, string Message);

public interface IDoctorCheck
{
    IEnumerable<CheckResult> Run(Infrastructure.SolutionContext solution);
}
