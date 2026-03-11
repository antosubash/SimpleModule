using System.ComponentModel;
using Spectre.Console.Cli;

namespace SimpleModule.Cli.Commands.Doctor;

public sealed class DoctorSettings : CommandSettings
{
    [CommandOption("--fix")]
    [Description("Auto-fix missing slnx entries and project references")]
    public bool Fix { get; set; }
}
