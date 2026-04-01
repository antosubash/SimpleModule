namespace SimpleModule.Agents.Middleware;

#pragma warning disable CA1711 // Identifiers should not have incorrect suffix - Delegate naming is intentional
public delegate Task AgentMiddlewareDelegate(AgentContext context);
#pragma warning restore CA1711

public interface IAgentMiddleware
{
    Task InvokeAsync(AgentContext context, AgentMiddlewareDelegate next);
}
