using Microsoft.Extensions.Logging;

namespace SimpleModule.DevTools;

public sealed partial class ViteDevWatchService
{
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Could not find repository root (.git directory). ViteDevWatchService will not start."
    )]
    private static partial void LogRepoRootNotFound(ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "ViteDevWatchService starting. Repo root: {RepoRoot}"
    )]
    private static partial void LogServiceStarting(ILogger logger, string repoRoot);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "ViteDevWatchService watching {ModuleCount} module(s), ClientApp, and Styles"
    )]
    private static partial void LogWatchingStarted(ILogger logger, int moduleCount);

    [LoggerMessage(Level = LogLevel.Debug, Message = "{BuildKey} file changed: {FilePath}")]
    private static partial void LogFileChanged(ILogger logger, string buildKey, string? filePath);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Watching module: {ModuleName} at {ModuleDir}"
    )]
    private static partial void LogWatchingModule(
        ILogger logger,
        string moduleName,
        string moduleDir
    );

    [LoggerMessage(Level = LogLevel.Debug, Message = "Watching ClientApp at {ClientAppDir}")]
    private static partial void LogWatchingClientApp(ILogger logger, string clientAppDir);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Watching Styles at {StylesDir}")]
    private static partial void LogWatchingStyles(ILogger logger, string stylesDir);

    [LoggerMessage(Level = LogLevel.Information, Message = "Rebuilding {Name}...")]
    private static partial void LogRebuilding(ILogger logger, string name);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "{Name} rebuilt successfully in {ElapsedMs}ms"
    )]
    private static partial void LogRebuiltSuccessfully(ILogger logger, string name, long elapsedMs);

    [LoggerMessage(Level = LogLevel.Error, Message = "{Name} build failed")]
    private static partial void LogBuildFailed(ILogger logger, string name);

    [LoggerMessage(Level = LogLevel.Error, Message = "Build stderr: {Stderr}")]
    private static partial void LogBuildStderr(ILogger logger, string stderr);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Build stdout: {Stdout}")]
    private static partial void LogBuildStdout(ILogger logger, string stdout);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Build output: {Stdout}")]
    private static partial void LogBuildOutput(ILogger logger, string stdout);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Build cancelled")]
    private static partial void LogBuildCancelled(ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to run build process: {FileName} {Arguments}"
    )]
    private static partial void LogBuildProcessFailed(
        ILogger logger,
        Exception ex,
        string fileName,
        string arguments
    );
}
