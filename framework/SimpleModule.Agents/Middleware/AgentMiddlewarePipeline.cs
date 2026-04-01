namespace SimpleModule.Agents.Middleware;

public sealed class AgentMiddlewarePipeline
{
    private readonly List<IAgentMiddleware> _middlewares = [];

    public AgentMiddlewarePipeline Use(IAgentMiddleware middleware)
    {
        _middlewares.Add(middleware);
        return this;
    }

    public AgentMiddlewareDelegate Build(AgentMiddlewareDelegate finalHandler)
    {
        var handler = finalHandler;
        for (var i = _middlewares.Count - 1; i >= 0; i--)
        {
            var middleware = _middlewares[i];
            var next = handler;
            handler = context => middleware.InvokeAsync(context, next);
        }

        return handler;
    }
}
