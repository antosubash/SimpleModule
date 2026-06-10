using System.Diagnostics;

namespace SimpleModule.Cli.Infrastructure;

public sealed record ProcessResult(int ExitCode, string Output, string Error)
{
    public bool Success => ExitCode == 0;
}

/// <summary>Runs external tools (dotnet, npx) capturing output.</summary>
public static class ProcessRunner
{
    public static async Task<ProcessResult> RunAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        string? workingDirectory = null,
        IReadOnlyDictionary<string, string>? environment = null
    )
    {
        // npm/npx are .cmd shims on Windows; CreateProcess cannot start them
        // directly (and .NET blocks cmd files with ArgumentList), so route
        // through cmd.exe there.
        var actualArguments = arguments;
        if (OperatingSystem.IsWindows() && fileName is "npx" or "npm")
        {
            actualArguments = ["/c", fileName, .. arguments];
            fileName = "cmd.exe";
        }

        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory,
        };
        foreach (var argument in actualArguments)
        {
            psi.ArgumentList.Add(argument);
        }

        if (environment is not null)
        {
            foreach (var kvp in environment)
            {
                psi.Environment[kvp.Key] = kvp.Value;
            }
        }

        using var process = new Process { StartInfo = psi };
        process.Start();
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync().ConfigureAwait(false);

        return new ProcessResult(
            process.ExitCode,
            await outputTask.ConfigureAwait(false),
            await errorTask.ConfigureAwait(false)
        );
    }
}
