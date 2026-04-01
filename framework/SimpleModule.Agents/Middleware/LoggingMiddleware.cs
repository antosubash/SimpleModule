using Microsoft.Extensions.Logging;

namespace SimpleModule.Agents.Middleware;

public sealed partial class LoggingMiddleware(ILogger<LoggingMiddleware> logger) : IAgentMiddleware
{
    public async Task InvokeAsync(AgentContext context, AgentMiddlewareDelegate next)
    {
        LogAgentInvocation(logger, context.AgentName, context.Request.Message.Length);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await next(context);
        stopwatch.Stop();

        LogAgentResponse(logger, context.AgentName, stopwatch.ElapsedMilliseconds);
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Agent '{AgentName}' invoked with {MessageLength} char message"
    )]
    private static partial void LogAgentInvocation(
        ILogger logger,
        string agentName,
        int messageLength
    );

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Agent '{AgentName}' responded in {ElapsedMs}ms"
    )]
    private static partial void LogAgentResponse(ILogger logger, string agentName, long elapsedMs);
}
