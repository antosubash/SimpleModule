using System.Diagnostics;

namespace SimpleModule.Datasets.Infrastructure;

/// <summary>
/// Runs a CLI tool asynchronously. Reads stdout and stderr concurrently to avoid
/// pipe-buffer deadlocks, and throws on non-zero exit codes.
/// </summary>
internal static class CliRunner
{
    public static async Task<string> RunAsync(
        string fileName,
        IEnumerable<string> args,
        CancellationToken ct
    )
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = fileName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var arg in args)
        {
            process.StartInfo.ArgumentList.Add(arg);
        }

        process.Start();

        // Read both streams concurrently to prevent deadlocks when either
        // pipe buffer fills before the other is drained.
        var stdoutTask = process.StandardOutput.ReadToEndAsync(ct);
        var stderrTask = process.StandardError.ReadToEndAsync(ct);
        await Task.WhenAll(stdoutTask, stderrTask);
        await process.WaitForExitAsync(ct);

        var stderr = await stderrTask;
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"{fileName} exited with code {process.ExitCode}: {stderr}"
            );
        }

        return await stdoutTask;
    }
}
