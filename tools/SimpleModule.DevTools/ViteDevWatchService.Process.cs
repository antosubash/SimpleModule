using System.Diagnostics;

namespace SimpleModule.DevTools;

public sealed partial class ViteDevWatchService
{
    private async Task<bool> RunProcessAsync(
        string fileName,
        string arguments,
        string workingDirectory,
        CancellationToken stoppingToken
    )
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            process.StartInfo.Environment["VITE_MODE"] = "dev";

            var existingPath = process.StartInfo.Environment.TryGetValue("PATH", out var path)
                ? path
                : "";
            process.StartInfo.Environment["PATH"] =
                $"{_npmBinPath}{Path.PathSeparator}{existingPath}";

            process.Start();

            var stdoutTask = process.StandardOutput.ReadToEndAsync(stoppingToken);
            var stderrTask = process.StandardError.ReadToEndAsync(stoppingToken);

            await process.WaitForExitAsync(stoppingToken).ConfigureAwait(false);

            var stdout = await stdoutTask.ConfigureAwait(false);
            var stderr = await stderrTask.ConfigureAwait(false);

            if (process.ExitCode != 0)
            {
                var trimmedStderr = stderr.Trim();
                if (trimmedStderr.Length > 0)
                {
                    LogBuildStderr(logger, trimmedStderr);
                }

                var trimmedStdout = stdout.Trim();
                if (trimmedStdout.Length > 0)
                {
                    LogBuildStdout(logger, trimmedStdout);
                }

                return false;
            }

            var trimmedOutput = stdout.Trim();
            if (trimmedOutput.Length > 0)
            {
                LogBuildOutput(logger, trimmedOutput);
            }

            return true;
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            LogBuildCancelled(logger);
            return false;
        }
#pragma warning disable CA1031 // Do not catch general exception types
        catch (Exception ex)
#pragma warning restore CA1031
        {
            LogBuildProcessFailed(logger, ex, fileName, arguments);
            return false;
        }
    }
}
