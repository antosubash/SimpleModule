using Microsoft.Extensions.Logging;

namespace SimpleModule.Agents.Middleware;

public sealed partial class TokenTrackingMiddleware(ILogger<TokenTrackingMiddleware> logger)
    : IAgentMiddleware
{
    public async Task InvokeAsync(AgentContext context, AgentMiddlewareDelegate next)
    {
        await next(context);

        if (context.Response is not null)
        {
            var estimatedTokens =
                EstimateTokens(context.Request.Message) + EstimateTokens(context.Response.Message);
            context.Properties["EstimatedTokens"] = estimatedTokens;
            LogTokenUsage(logger, context.AgentName, estimatedTokens);
        }
    }

    private static int EstimateTokens(string text) => text.Length / 4;

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Agent '{AgentName}' used ~{EstimatedTokens} tokens"
    )]
    private static partial void LogTokenUsage(
        ILogger logger,
        string agentName,
        int estimatedTokens
    );
}
