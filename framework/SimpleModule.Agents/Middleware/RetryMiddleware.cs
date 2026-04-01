using Microsoft.Extensions.Logging;

namespace SimpleModule.Agents.Middleware;

public sealed partial class RetryMiddleware(ILogger<RetryMiddleware> logger) : IAgentMiddleware
{
    private const int MaxRetries = 3;
    private static readonly TimeSpan[] Delays =
    [
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(4),
    ];

    public async Task InvokeAsync(AgentContext context, AgentMiddlewareDelegate next)
    {
        for (var attempt = 0; attempt <= MaxRetries; attempt++)
        {
            try
            {
                await next(context);
                return;
            }
            catch (HttpRequestException) when (attempt < MaxRetries)
            {
                LogRetry(logger, context.AgentName, attempt + 1, MaxRetries);
                await Task.Delay(Delays[attempt], context.CancellationToken);
            }
        }
    }

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Agent '{AgentName}' retry {Attempt}/{MaxRetries} after transient failure"
    )]
    private static partial void LogRetry(
        ILogger logger,
        string agentName,
        int attempt,
        int maxRetries
    );
}
